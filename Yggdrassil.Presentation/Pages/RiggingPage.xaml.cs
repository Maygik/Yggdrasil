using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.Linq;
using Windows.ApplicationModel.DataTransfer;
using Yggdrassil.Domain.Rigging;
using Yggdrassil.Domain.Scene;
using Yggdrassil.Presentation.Models;
using Yggdrassil.Presentation.Services;

namespace Yggdrassil.Presentation.Pages
{
    public sealed partial class RiggingPage : Page
    {
        public AppHost Host { get; }
        public ObservableCollection<RigSlotSectionItem> SlotSections { get; } = new();
        public ObservableCollection<RigSlotEditorItem> AssignableSlots { get; } = new();

        private readonly RigSlotSectionItem _mainSlotSection = new() { Title = "Main" };
        private readonly RigSlotSectionItem _limbSlotSection = new() { Title = "Limbs" };
        private readonly RigSlotSectionItem _leftHandSlotSection = new() { Title = "Left Hand" };
        private readonly RigSlotSectionItem _rightHandSlotSection = new() { Title = "Right Hand" };
        private readonly Dictionary<TreeViewNode, BoneHierarchyItem> _boneLookupByNode = new();
        private BoneHierarchyItem? _selectedBone;

        public RiggingPage()
        {
            Host = App.Instance.Host;
            InitializeComponent();

            InitializeSlotSections();
            ResetSelectionUi();

            Host.Shell.PropertyChanged += Shell_PropertyChanged;
            Unloaded += RiggingPage_Unloaded;

            RefreshFromShell();
        }

        private void Shell_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(Host.Shell.CurrentSession)
                || e.PropertyName == nameof(Host.Shell.HasOpenProject)
                || e.PropertyName == nameof(Host.Shell.CanExportModel))
            {
                RefreshFromShell();
            }
        }

        private void RefreshFromShell()
        {
            NoProjectPanel.Visibility = Host.Shell.HasOpenProject ? Visibility.Collapsed : Visibility.Visible;
            RiggingSettingsPanel.Visibility = Host.Shell.HasOpenProject ? Visibility.Visible : Visibility.Collapsed;

            PopulateRigSlots();

            var scene = Host.Shell.CurrentSession?.Project?.Scene;
            var rootBone = scene?.RootBone;
            var hasSkeleton = rootBone is not null;

            NoSkeletonPanel.Visibility = hasSkeleton ? Visibility.Collapsed : Visibility.Visible;
            BoneHierarchyScrollViewer.Visibility = hasSkeleton ? Visibility.Visible : Visibility.Collapsed;
            BoneHierarchyPanel.Visibility = hasSkeleton ? Visibility.Visible : Visibility.Collapsed;

            if (rootBone is not null)
                PopulateBoneHierarchy(rootBone);
            else
                ClearBoneHierarchy();
        }

        private void PopulateBoneHierarchy(Bone rootBone)
        {
            _boneLookupByNode.Clear();
            BoneHierarchyTreeView.RootNodes.Clear();
            BoneHierarchyTreeView.RootNodes.Add(CreateBoneTreeNode(rootBone, true));
            SetSelectedBone(null);
        }

        private void ClearBoneHierarchy()
        {
            _boneLookupByNode.Clear();
            BoneHierarchyTreeView.RootNodes.Clear();
            SetSelectedBone(null);
        }

        private TreeViewNode CreateBoneTreeNode(Bone bone, bool isExpanded = false)
        {
            var boneItem = new BoneHierarchyItem
            {
                Name = bone.Name,
                IsDeform = bone.IsDeform
            };

            var node = new TreeViewNode
            {
                Content = bone.Name,
                IsExpanded = isExpanded
            };
            _boneLookupByNode[node] = boneItem;

            foreach (var child in bone.Children)
            {
                if (child is Bone childBone)
                {
                    node.Children.Add(CreateBoneTreeNode(childBone, true));
                }
            }

            return node;
        }

        private void PopulateRigSlots()
        {
            var selectedSlotIndex = AssignBoneComboBox.SelectedItem is RigSlotEditorItem selectedSlot
                ? selectedSlot.SlotIndex
                : (int?)null;

            _mainSlotSection.Slots.Clear();
            _limbSlotSection.Slots.Clear();
            _leftHandSlotSection.Slots.Clear();
            _rightHandSlotSection.Slots.Clear();
            AssignableSlots.Clear();

            var rigMapping = Host.Shell.CurrentSession?.Project?.RigMapping;
            if (rigMapping is null)
            {
                AssignBoneComboBox.SelectedItem = null;
                UpdateAssignButtonState();
                return;
            }

            AddSlots(_mainSlotSection.Slots, rigMapping, _mainSlotSection.Title, 0, 6);
            AddSlots(_limbSlotSection.Slots, rigMapping, _limbSlotSection.Title, 7, 22);
            AddSlots(_leftHandSlotSection.Slots, rigMapping, _leftHandSlotSection.Title, 23, 37);
            AddSlots(_rightHandSlotSection.Slots, rigMapping, _rightHandSlotSection.Title, 38, 52);

            AssignBoneComboBox.SelectedItem = selectedSlotIndex.HasValue
                ? AssignableSlots.FirstOrDefault(slot => slot.SlotIndex == selectedSlotIndex.Value)
                : null;

            UpdateAssignButtonState();
        }

        private void InitializeSlotSections()
        {
            SlotSections.Clear();
            SlotSections.Add(_mainSlotSection);
            SlotSections.Add(_limbSlotSection);
            SlotSections.Add(_leftHandSlotSection);
            SlotSections.Add(_rightHandSlotSection);
        }

        private void AddSlots(
            ObservableCollection<RigSlotEditorItem> target,
            SourceBoneMapping rigMapping,
            string sectionTitle,
            int startIndex,
            int endIndex)
        {
            for (int i = startIndex; i <= endIndex; i++)
            {
                var slot = rigMapping[i];
                var editorItem = new RigSlotEditorItem
                {
                    SlotIndex = i,
                    SectionTitle = sectionTitle,
                    DisplayName = slot.DisplayName,
                    LogicalBone = slot.LogicalBone
                };

                editorItem.UpdateAssignment(slot.AssignedBone);
                target.Add(editorItem);
                AssignableSlots.Add(editorItem);
            }
        }

        private void BoneHierarchyTreeView_SelectionChanged(TreeView sender, TreeViewSelectionChangedEventArgs args)
        {
            var selectedBone = sender.SelectedNode is not null
                && _boneLookupByNode.TryGetValue(sender.SelectedNode, out var selectedItem)
                ? selectedItem
                : null;
            SetSelectedBone(selectedBone);
        }

        private void BoneHierarchyTreeView_DragItemsStarting(TreeView sender, TreeViewDragItemsStartingEventArgs args)
        {
            var selectedBone = args.Items
                .OfType<TreeViewNode>()
                .Select(node => _boneLookupByNode.TryGetValue(node, out var boneItem) ? boneItem : null)
                .FirstOrDefault(boneItem => boneItem is not null)
                ?? (sender.SelectedNode is not null
                    && _boneLookupByNode.TryGetValue(sender.SelectedNode, out var selectedItem)
                        ? selectedItem
                        : null);
            if (selectedBone is null)
            {
                args.Cancel = true;
                return;
            }

            args.Data.SetText(selectedBone.Name);
        }

        private void AssignBoneComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            UpdateAssignButtonState();
        }

        private void AssignSelectedBoneButton_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedBone is null)
            {
                Host.Shell.StatusMessage = "Select a bone before assigning it.";
                UpdateAssignButtonState();
                return;
            }

            if (AssignBoneComboBox.SelectedItem is not RigSlotEditorItem selectedSlot)
            {
                Host.Shell.StatusMessage = "Select a rig slot before assigning the bone.";
                UpdateAssignButtonState();
                return;
            }

            AssignBoneToSlot(_selectedBone.Name, selectedSlot);
        }

        private void RigSlotAssignedField_DragOver(object sender, DragEventArgs e)
        {
            e.AcceptedOperation = e.DataView.Contains(StandardDataFormats.Text)
                ? DataPackageOperation.Copy
                : DataPackageOperation.None;
        }

        private async void RigSlotAssignedField_Drop(object sender, DragEventArgs e)
        {
            if (sender is not FrameworkElement element || element.Tag is not RigSlotEditorItem slotItem)
                return;

            if (!e.DataView.Contains(StandardDataFormats.Text))
                return;

            var boneName = (await e.DataView.GetTextAsync()).Trim();
            if (string.IsNullOrWhiteSpace(boneName))
                return;

            AssignBoneComboBox.SelectedItem = AssignableSlots.FirstOrDefault(slot => slot.SlotIndex == slotItem.SlotIndex);
            AssignBoneToSlot(boneName, slotItem);
        }

        private void AssignBoneToSlot(string boneName, RigSlotEditorItem slotItem)
        {
            var project = Host.Shell.CurrentSession?.Project;
            if (project is null)
            {
                Host.Shell.StatusMessage = "No project is currently open.";
                return;
            }

            var result = Host.Backend.ProjectEditor.BindBone(project, boneName, slotItem.SlotIndex);
            Host.Shell.StatusMessage = result.Success
                ? result.Messages.FirstOrDefault() ?? $"Bound model bone '{boneName}' to slot '{slotItem.DisplayName}'."
                : result.ErrorMessage ?? "Failed to bind the selected bone.";

            if (!result.Success)
                return;

            slotItem.UpdateAssignment(boneName);
        }

        private void ResetSelectionUi()
        {
            SelectedBoneTextBlock.Text = "Selected bone: None";
            AssignSelectedBoneButton.IsEnabled = false;
        }

        private void SetSelectedBone(BoneHierarchyItem? boneItem)
        {
            _selectedBone = boneItem;
            SelectedBoneTextBlock.Text = boneItem is null
                ? "Selected bone: None"
                : $"Selected bone: {boneItem.Name}";

            UpdateAssignButtonState();
        }

        private void UpdateAssignButtonState()
        {
            AssignSelectedBoneButton.IsEnabled =
                _selectedBone is not null && AssignBoneComboBox.SelectedItem is RigSlotEditorItem;
        }

        private void RiggingPage_Unloaded(object sender, RoutedEventArgs e)
        {
            Host.Shell.PropertyChanged -= Shell_PropertyChanged;
        }
    }
}
