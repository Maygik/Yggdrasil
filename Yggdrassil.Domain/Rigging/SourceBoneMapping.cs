using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Yggdrassil.Domain.Rigging
{
    /// <summary>
    /// Maps RigSlots to imported bone names.
    /// Only stores the relationship, no validation or auto-mapping logic.
    /// </summary>
    public class SourceBoneMapping
    {
        public RigSlot GetRigSlotFromName(string name)
        {
            if (HeadAlias.Contains(name, StringComparer.OrdinalIgnoreCase))
                return HeadSlot;

            // Add additional mappings as needed, e.g.:
            if (NeckAlias.Contains(name, StringComparer.OrdinalIgnoreCase))
                return NeckSlot;

            // ...add more mappings for other slots/aliases here...

            throw new ArgumentException($"No RigSlot found for name: {name}", nameof(name));
        }

        /// <summary>
        /// Provides an enumerator to iterate through all RigSlots in this mapping.
        /// </summary>
        /// <returns>An enumerator of all RigSlots.</returns>
        public IEnumerator<RigSlot> GetEnumerator()
        {
            yield return HeadSlot;
            yield return NeckSlot;
            yield return Spine4Slot;
            yield return Spine2Slot;
            yield return Spine1Slot;
            yield return SpineSlot;
            yield return PelvisSlot;

            yield return LeftClavicleSlot;
            yield return LeftUpperArmSlot;
            yield return LeftForearmSlot;
            yield return LeftHandSlot;

            yield return RightClavicleSlot;
            yield return RightUpperArmSlot;
            yield return RightForearmSlot;
            yield return RightHandSlot;

            yield return LeftThighSlot;
            yield return LeftCalfSlot;
            yield return LeftFootSlot;
            yield return LeftToesSlot;

            yield return RightThighSlot;
            yield return RightCalfSlot;
            yield return RightFootSlot;
            yield return RightToesSlot;

            yield return LeftHandThumb1Slot;
            yield return LeftHandThumb2Slot;
            yield return LeftHandThumb3Slot;
            yield return LeftHandIndex1Slot;
            yield return LeftHandIndex2Slot;
            yield return LeftHandIndex3Slot;
            yield return LeftHandMiddle1Slot;
            yield return LeftHandMiddle2Slot;
            yield return LeftHandMiddle3Slot;
            yield return LeftHandRing1Slot;
            yield return LeftHandRing2Slot;
            yield return LeftHandRing3Slot;
            yield return LeftHandPinky1Slot;
            yield return LeftHandPinky2Slot;
            yield return LeftHandPinky3Slot;

            yield return RightHandThumb1Slot;
            yield return RightHandThumb2Slot;
            yield return RightHandThumb3Slot;
            yield return RightHandIndex1Slot;
            yield return RightHandIndex2Slot;
            yield return RightHandIndex3Slot;
            yield return RightHandMiddle1Slot;
            yield return RightHandMiddle2Slot;
            yield return RightHandMiddle3Slot;
            yield return RightHandRing1Slot;
            yield return RightHandRing2Slot;
            yield return RightHandRing3Slot;
            yield return RightHandPinky1Slot;
            yield return RightHandPinky2Slot;
            yield return RightHandPinky3Slot;
        }


        public RigSlot HeadSlot { get; set; } = new RigSlot(                "ValveBiped.Bip01_Head1",           "Head");
        public string[] HeadAlias = { "valvebiped.bip01_head", "valvebiped.bip01_head1", "head",  };
        public RigSlot NeckSlot { get; set; } = new RigSlot(                "ValveBiped.Bip01_Neck1",           "Neck");
        public string[] NeckAlias = { "valvebiped.bip01_neck", "valvebiped.bip01_neck1", "neck",  };


        public RigSlot Spine4Slot { get; set; } = new RigSlot(              "ValveBiped.Bip01_Spine4",          "Upper Chest");
        public string[] Spine4Alias = { "valvebiped.bip01_spine4", "valvebiped.bip01_spine03", "spine4", "spine03", "upperchest", "upper chest", "chest" };
        public RigSlot Spine2Slot { get; set; } = new RigSlot(              "ValveBiped.Bip01_Spine2",          "Chest");
        public string[] Spine2Alias = { "valvebiped.bip01_spine2", "valvebiped.bip01_spine02", "spine2", "spine02", "chest" };
        public RigSlot Spine1Slot { get; set; } = new RigSlot(              "ValveBiped.Bip01_Spine1",          "Central Spine");
        public string[] Spine1Alias = { "valvebiped.bip01_spine1", "valvebiped.bip01_spine01", "spine1", "spine01", "central spine" };
        public RigSlot SpineSlot { get; set; } = new RigSlot(               "ValveBiped.Bip01_Spine",           "Lower Spine");
        public string[] SpineAlias = { "valvebiped.bip01_spine", "valvebiped.bip01_spine0", "spine", "spine0", "lowerspine", "lower spine" };
        public RigSlot PelvisSlot { get; set; } = new RigSlot(              "ValveBiped.Bip01_Pelvis",          "Pelvis");
        public string[] PelvisAlias = { "valvebiped.bip01_pelvis", "pelvis", "hips" };

        public RigSlot LeftClavicleSlot { get; set; } = new RigSlot(        "ValveBiped.Bip01_L_Clavicle",      "Left Clavicle");
        public string[] LeftClavicleAlias = { "valvebiped.bip01_l_clavicle", "valvebiped.bip01_l_clav", "left clavicle", "left clav", "left shoulder" };
        public RigSlot LeftUpperArmSlot { get; set; } = new RigSlot(        "ValveBiped.Bip01_L_UpperArm",      "Left Upper Arm");
        public string[] LeftUpperArmAlias = { "valvebiped.bip01_l_upperarm", "valvebiped.bip01_l_arm", "left upperarm", "left arm" };
        public RigSlot LeftForearmSlot { get; set; } = new RigSlot(         "ValveBiped.Bip01_L_Forearm",       "Left Forearm");
        public string[] LeftForearmAlias = { "valvebiped.bip01_l_forearm", "valvebiped.bip01_l_forearm", "left forearm", "left forearm" };
        public RigSlot LeftHandSlot { get; set; } = new RigSlot(            "ValveBiped.Bip01_L_Hand",          "Left Hand");
        public string[] LeftHandAlias = { "valvebiped.bip01_l_hand", "left hand", "left wrist" };

        public RigSlot RightClavicleSlot { get; set; } = new RigSlot(       "ValveBiped.Bip01_R_Clavicle",      "Right Clavicle");
        public string[] RightClavicleAlias = { "valvebiped.bip01_r_clavicle", "valvebiped.bip01_r_clav", "right clavicle", "right clav", "right shoulder" };
        public RigSlot RightUpperArmSlot { get; set; } = new RigSlot(       "ValveBiped.Bip01_R_UpperArm",      "Right Upper Arm");
        public string[] RightUpperArmAlias = { "valvebiped.bip01_r_upperarm", "valvebiped.bip01_r_arm", "right upperarm", "right arm" };
        public RigSlot RightForearmSlot { get; set; } = new RigSlot(        "ValveBiped.Bip01_R_Forearm",       "Right Forearm");
        public string[] RightForearmAlias = { "valvebiped.bip01_r_forearm", "valvebiped.bip01_r_forearm", "right forearm", "right forearm" };
        public RigSlot RightHandSlot { get; set; } = new RigSlot(           "ValveBiped.Bip01_R_Hand",          "Right Hand");
        public string[] RightHandAlias = { "valvebiped.bip01_r_hand", "right hand", "right wrist" };

        public RigSlot LeftThighSlot { get; set; } = new RigSlot(           "ValveBiped.Bip01_L_Thigh",         "Left Thigh");
        public string[] LeftThighAlias = { "valvebiped.bip01_l_thigh", "valvebiped.bip01_l_leg", "left thigh", "left leg" };
        public RigSlot LeftCalfSlot { get; set; } = new RigSlot(            "ValveBiped.Bip01_L_Calf",          "Left Calf");
        public string[] LeftCalfAlias = { "valvebiped.bip01_l_calf", "valvebiped.bip01_l_leg", "left calf", "left leg" };
        public RigSlot LeftFootSlot { get; set; } = new RigSlot(            "ValveBiped.Bip01_L_Foot",          "Left Foot");
        public string[] LeftFootAlias = { "valvebiped.bip01_l_foot", "left foot", "left ankle" };
        public RigSlot LeftToesSlot { get; set; } = new RigSlot(            "ValveBiped.Bip01_L_Toe0",          "Left Toes");
        public string[] LeftToesAlias = { "valvebiped.bip01_l_toe0", "left toes", "left toe" };

        public RigSlot RightThighSlot { get; set; } = new RigSlot(          "ValveBiped.Bip01_R_Thigh",         "Right Thigh");
        public string[] RightThighAlias = { "valvebiped.bip01_r_thigh", "valvebiped.bip01_r_leg", "right thigh", "right leg" };
        public RigSlot RightCalfSlot { get; set; } = new RigSlot(           "ValveBiped.Bip01_R_Calf",          "Right Calf");
        public string[] RightCalfAlias = { "valvebiped.bip01_r_calf", "valvebiped.bip01_r_leg", "right calf", "right leg" };
        public RigSlot RightFootSlot { get; set; } = new RigSlot(           "ValveBiped.Bip01_R_Foot",          "Right Foot");
        public string[] RightFootAlias = { "valvebiped.bip01_r_foot", "right foot", "right ankle" };
        public RigSlot RightToesSlot { get; set; } = new RigSlot(           "ValveBiped.Bip01_R_Toe0",          "Right Toes");
        public string[] RightToesAlias = { "valvebiped.bip01_r_toe0", "right toes", "right toe" };


        // Fingies
        public RigSlot LeftHandThumb1Slot { get; set; } = new RigSlot(      "ValveBiped.Bip01_L_Finger0",       "Left Hand Thumb 1");
        public RigSlot LeftHandThumb2Slot { get; set; } = new RigSlot(      "ValveBiped.Bip01_L_Finger01",      "Left Hand Thumb 2");
        public RigSlot LeftHandThumb3Slot { get; set; } = new RigSlot(      "ValveBiped.Bip01_L_Finger02",      "Left Hand Thumb 3");
        public RigSlot LeftHandIndex1Slot { get; set; } = new RigSlot(      "ValveBiped.Bip01_L_Finger1",       "Left Hand Index 1");
        public RigSlot LeftHandIndex2Slot { get; set; } = new RigSlot(      "ValveBiped.Bip01_L_Finger11",      "Left Hand Index 2");
        public RigSlot LeftHandIndex3Slot { get; set; } = new RigSlot(      "ValveBiped.Bip01_L_Finger12",      "Left Hand Index 3");
        public RigSlot LeftHandMiddle1Slot { get; set; } = new RigSlot(     "ValveBiped.Bip01_L_Finger2",       "Left Hand Middle 1");
        public RigSlot LeftHandMiddle2Slot { get; set; } = new RigSlot(     "ValveBiped.Bip01_L_Finger21",      "Left Hand Middle 2");
        public RigSlot LeftHandMiddle3Slot { get; set; } = new RigSlot(     "ValveBiped.Bip01_L_Finger22",      "Left Hand Middle 3");
        public RigSlot LeftHandRing1Slot { get; set; } = new RigSlot(       "ValveBiped.Bip01_L_Finger3",       "Left Hand Ring 1");
        public RigSlot LeftHandRing2Slot { get; set; } = new RigSlot(       "ValveBiped.Bip01_L_Finger31",      "Left Hand Ring 2");
        public RigSlot LeftHandRing3Slot { get; set; } = new RigSlot(       "ValveBiped.Bip01_L_Finger32",      "Left Hand Ring 3");
        public RigSlot LeftHandPinky1Slot { get; set; } = new RigSlot(      "ValveBiped.Bip01_L_Finger4",       "Left Hand Pinky 1");
        public RigSlot LeftHandPinky2Slot { get; set; } = new RigSlot(      "ValveBiped.Bip01_L_Finger41",      "Left Hand Pinky 2");
        public RigSlot LeftHandPinky3Slot { get; set; } = new RigSlot(      "ValveBiped.Bip01_L_Finger42",      "Left Hand Pinky 3");
        public RigSlot RightHandThumb1Slot { get; set; } = new RigSlot(     "ValveBiped.Bip01_R_Finger0",       "Right Hand Thumb 1");
        public RigSlot RightHandThumb2Slot { get; set; } = new RigSlot(     "ValveBiped.Bip01_R_Finger01",      "Right Hand Thumb 2");
        public RigSlot RightHandThumb3Slot { get; set; } = new RigSlot(     "ValveBiped.Bip01_R_Finger02",      "Right Hand Thumb 3");
        public RigSlot RightHandIndex1Slot { get; set; } = new RigSlot(     "ValveBiped.Bip01_R_Finger1",       "Right Hand Index 1");
        public RigSlot RightHandIndex2Slot { get; set; } = new RigSlot(     "ValveBiped.Bip01_R_Finger11",      "Right Hand Index 2");
        public RigSlot RightHandIndex3Slot { get; set; } = new RigSlot(     "ValveBiped.Bip01_R_Finger12",      "Right Hand Index 3");
        public RigSlot RightHandMiddle1Slot { get; set; } = new RigSlot(    "ValveBiped.Bip01_R_Finger2",       "Right Hand Middle 1");
        public RigSlot RightHandMiddle2Slot { get; set; } = new RigSlot(    "ValveBiped.Bip01_R_Finger21",      "Right Hand Middle 2");
        public RigSlot RightHandMiddle3Slot { get; set; } = new RigSlot(    "ValveBiped.Bip01_R_Finger22",      "Right Hand Middle 3");
        public RigSlot RightHandRing1Slot { get; set; } = new RigSlot(      "ValveBiped.Bip01_R_Finger3",       "Right Hand Ring 1");
        public RigSlot RightHandRing2Slot { get; set; } = new RigSlot(      "ValveBiped.Bip01_R_Finger31",      "Right Hand Ring 2");
        public RigSlot RightHandRing3Slot { get; set; } = new RigSlot(      "ValveBiped.Bip01_R_Finger32",      "Right Hand Ring 3");
        public RigSlot RightHandPinky1Slot { get; set; } = new RigSlot(     "ValveBiped.Bip01_R_Finger4",       "Right Hand Pinky 1");
        public RigSlot RightHandPinky2Slot { get; set; } = new RigSlot(     "ValveBiped.Bip01_R_Finger41",      "Right Hand Pinky 2");
        public RigSlot RightHandPinky3Slot { get; set; } = new RigSlot(     "ValveBiped.Bip01_R_Finger42",      "Right Hand Pinky 3");

    }
}
