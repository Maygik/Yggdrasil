using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.Linq;
using Yggdrasil.Domain.Rigging;
using Yggdrasil.Domain.Scene;
using Yggdrasil.Presentation.Models;
using Yggdrasil.Presentation.Services;

namespace Yggdrasil.Presentation.Pages
{
    public sealed partial class RiggingPage : Page
    {
        public AppHost Host { get; }
        public ObservableCollection<RigSlotSectionItem> SlotSections { get; } = new();

        private readonly RigSlotSectionItem _mainSlotSection = new() { Title = "Main" };
        private readonly RigSlotSectionItem _limbSlotSection = new() { Title = "Limbs" };
        private readonly RigSlotSectionItem _leftHandSlotSection = new() { Title = "Left Hand" };
        private readonly RigSlotSectionItem _rightHandSlotSection = new() { Title = "Right Hand" };
        private readonly Dictionary<TreeViewNode, BoneHierarchyItem> _boneLookupByNode = new();
        private BoneHierarchyItem? _selectedBone;
        private RigSlotEditorItem? _selectedSlot;

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
            var selectedSlotIndex = _selectedSlot?.SlotIndex;

            _mainSlotSection.Slots.Clear();
            _limbSlotSection.Slots.Clear();
            _leftHandSlotSection.Slots.Clear();
            _rightHandSlotSection.Slots.Clear();

            var rigMapping = Host.Shell.CurrentSession?.Project?.RigMapping;
            if (rigMapping is null)
            {
                SetSelectedSlot(null);
                return;
            }

            AddSlots(_mainSlotSection.Slots, rigMapping, _mainSlotSection.Title, 0, 6);
            AddSlots(_limbSlotSection.Slots, rigMapping, _limbSlotSection.Title, 7, 22);
            AddSlots(_leftHandSlotSection.Slots, rigMapping, _leftHandSlotSection.Title, 23, 37);
            AddSlots(_rightHandSlotSection.Slots, rigMapping, _rightHandSlotSection.Title, 38, 52);
            UpdateDuplicateAssignmentIndicators();
            SetSelectedSlot(selectedSlotIndex.HasValue
                ? SlotSections.SelectMany(section => section.Slots).FirstOrDefault(slot => slot.SlotIndex == selectedSlotIndex.Value)
                : null);
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

        private void RigSlotRow_Tapped(object sender, Microsoft.UI.Xaml.Input.TappedRoutedEventArgs e)
        {
            if (sender is FrameworkElement element && element.Tag is RigSlotEditorItem slotItem)
            {
                SetSelectedSlot(slotItem);
            }
        }

        private void BindSelectedBoneToSlotButton_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedBone is null)
            {
                Host.Shell.StatusMessage = "Select a bone before binding it to a rig slot.";
                return;
            }

            if (sender is not FrameworkElement element || element.Tag is not RigSlotEditorItem slotItem)
            {
                Host.Shell.StatusMessage = "Unable to determine which rig slot to bind.";
                return;
            }

            SetSelectedSlot(slotItem);
            AssignBoneToSlot(_selectedBone.Name, slotItem);
        }

        private void UnbindSelectedButton_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedSlot is null)
            {
                Host.Shell.StatusMessage = "Select a rig slot before unbinding it.";
                UpdateUnbindButtonState();
                return;
            }

            UnbindSlot(_selectedSlot);
        }

        private void UnbindAllButton_Click(object sender, RoutedEventArgs e)
        {
            var project = Host.Shell.CurrentSession?.Project;
            if (project is null)
            {
                Host.Shell.StatusMessage = "No project is currently open.";
                UpdateUnbindButtonState();
                return;
            }

            var assignedSlots = SlotSections
                .SelectMany(section => section.Slots)
                .Where(slot => slot.IsAssigned)
                .ToList();

            if (assignedSlots.Count == 0)
            {
                Host.Shell.StatusMessage = "There are no rig slot bindings to clear.";
                UpdateUnbindButtonState();
                return;
            }

            foreach (var slot in assignedSlots)
            {
                Host.Backend.ProjectEditor.UnbindBone(project, slot.SlotIndex);
                slot.UpdateAssignment(project.RigMapping[slot.SlotIndex].AssignedBone);
            }

            UpdateDuplicateAssignmentIndicators();
            UpdateUnbindButtonState();
            Host.Shell.StatusMessage = $"Cleared bindings for {assignedSlots.Count} rig slots.";
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

            slotItem.UpdateAssignment(project.RigMapping[slotItem.SlotIndex].AssignedBone);
            UpdateDuplicateAssignmentIndicators();
            UpdateUnbindButtonState();
        }

        private void UnbindSlot(RigSlotEditorItem slotItem)
        {
            var project = Host.Shell.CurrentSession?.Project;
            if (project is null)
            {
                Host.Shell.StatusMessage = "No project is currently open.";
                UpdateUnbindButtonState();
                return;
            }

            var result = Host.Backend.ProjectEditor.UnbindBone(project, slotItem.SlotIndex);
            Host.Shell.StatusMessage = result.Success
                ? result.Messages.FirstOrDefault() ?? $"Cleared binding for slot '{slotItem.DisplayName}'."
                : result.ErrorMessage ?? "Failed to clear the selected binding.";

            if (!result.Success)
                return;

            slotItem.UpdateAssignment(project.RigMapping[slotItem.SlotIndex].AssignedBone);
            UpdateDuplicateAssignmentIndicators();
            UpdateUnbindButtonState();
        }

        private void UpdateDuplicateAssignmentIndicators()
        {
            var duplicateAssignments = SlotSections
                .SelectMany(section => section.Slots)
                .Where(slot => !string.IsNullOrWhiteSpace(slot.AssignedBoneName))
                .GroupBy(slot => slot.AssignedBoneName!, StringComparer.Ordinal)
                .Where(group => group.Count() > 1)
                .Select(group => group.Key)
                .ToHashSet(StringComparer.Ordinal);

            foreach (var slot in SlotSections.SelectMany(section => section.Slots))
            {
                slot.UpdateDuplicateAssignment(
                    slot.AssignedBoneName is not null && duplicateAssignments.Contains(slot.AssignedBoneName));
            }
        }

        private void SetSelectedSlot(RigSlotEditorItem? slotItem)
        {
            if (ReferenceEquals(_selectedSlot, slotItem))
            {
                UpdateUnbindButtonState();
                return;
            }

            _selectedSlot?.UpdateSelection(false);
            _selectedSlot = slotItem;
            _selectedSlot?.UpdateSelection(true);
            UpdateUnbindButtonState();
        }

        private void UpdateUnbindButtonState()
        {
            if (UnbindSelectedButton is not null)
            {
                UnbindSelectedButton.IsEnabled = _selectedSlot?.IsAssigned == true;
            }

            if (UnbindAllButton is not null)
            {
                UnbindAllButton.IsEnabled = SlotSections
                    .SelectMany(section => section.Slots)
                    .Any(slot => slot.IsAssigned);
            }
        }

        private void ResetSelectionUi()
        {
            SelectedBoneTextBlock.Text = "Selected bone: None";
        }

        private void SetSelectedBone(BoneHierarchyItem? boneItem)
        {
            _selectedBone = boneItem;
            SelectedBoneTextBlock.Text = boneItem is null
                ? "Selected bone: None"
                : $"Selected bone: {boneItem.Name}";
        }

        private void RiggingPage_Unloaded(object sender, RoutedEventArgs e)
        {
            Host.Shell.PropertyChanged -= Shell_PropertyChanged;
        }
    }
}
