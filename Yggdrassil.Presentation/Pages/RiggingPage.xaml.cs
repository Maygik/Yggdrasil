using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System.Collections.ObjectModel;
using Yggdrassil.Domain.Rigging;
using Yggdrassil.Presentation.Models;
using Yggdrassil.Presentation.Services;

namespace Yggdrassil.Presentation.Pages
{
    public sealed partial class RiggingPage : Page
    {
        public AppHost Host { get; }
        public ObservableCollection<RigSlotSectionItem> SlotSections { get; } = new();

        private readonly RigSlotSectionItem _mainSlotSection = new() { Title = "Main" };
        private readonly RigSlotSectionItem _limbSlotSection = new() { Title = "Limbs" };
        private readonly RigSlotSectionItem _leftHandSlotSection = new() { Title = "Left Hand" };
        private readonly RigSlotSectionItem _rightHandSlotSection = new() { Title = "Right Hand" };

        public RiggingPage()
        {
            Host = App.Instance.Host;
            InitializeComponent();

            InitializeSlotSections();

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
            var hasSkeleton = scene?.RootBone is not null;

            NoSkeletonPanel.Visibility = hasSkeleton ? Visibility.Collapsed : Visibility.Visible;
            BoneHierarchyScrollViewer.Visibility = hasSkeleton ? Visibility.Visible : Visibility.Collapsed;
            BoneHierarchyPanel.Visibility = hasSkeleton ? Visibility.Visible : Visibility.Collapsed;
        }

        private void PopulateRigSlots()
        {
            _mainSlotSection.Slots.Clear();
            _limbSlotSection.Slots.Clear();
            _leftHandSlotSection.Slots.Clear();
            _rightHandSlotSection.Slots.Clear();

            var rigMapping = Host.Shell.CurrentSession?.Project?.RigMapping;
            if (rigMapping is null)
                return;

            AddSlots(_mainSlotSection.Slots, rigMapping, 0, 6);
            AddSlots(_limbSlotSection.Slots, rigMapping, 7, 22);
            AddSlots(_leftHandSlotSection.Slots, rigMapping, 23, 37);
            AddSlots(_rightHandSlotSection.Slots, rigMapping, 38, 52);
        }

        private void InitializeSlotSections()
        {
            SlotSections.Clear();
            SlotSections.Add(_mainSlotSection);
            SlotSections.Add(_limbSlotSection);
            SlotSections.Add(_leftHandSlotSection);
            SlotSections.Add(_rightHandSlotSection);
        }

        private static void AddSlots(ObservableCollection<RigSlotEditorItem> target, SourceBoneMapping rigMapping, int startIndex, int endIndex)
        {
            for (int i = startIndex; i <= endIndex; i++)
            {
                var slot = rigMapping[i];
                target.Add(new RigSlotEditorItem
                {
                    SlotIndex = i,
                    DisplayName = slot.DisplayName,
                    LogicalBone = slot.LogicalBone,
                    AssignedBoneDisplay = string.IsNullOrWhiteSpace(slot.AssignedBone) ? "Unassigned" : slot.AssignedBone,
                    IsAssigned = !string.IsNullOrWhiteSpace(slot.AssignedBone)
                });
            }
        }

        private void RiggingPage_Unloaded(object sender, RoutedEventArgs e)
        {
            Host.Shell.PropertyChanged -= Shell_PropertyChanged;
        }
    }
}
