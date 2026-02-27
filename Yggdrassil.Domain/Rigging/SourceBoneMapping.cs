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
        public RigSlot? TryGetRigSlotFromName(string name)
        {
            // Check if works as an integer index first
            if (int.TryParse(name, out int index))
            {
                if (index >= 0 && index < Count)
                    return this[index];
                else
                    throw new ArgumentOutOfRangeException(nameof(name), $"Index must be between 0 and {Count - 1}");
            }

            if (HeadAlias.Contains(name, StringComparer.OrdinalIgnoreCase))
                return HeadSlot;

            if (NeckAlias.Contains(name, StringComparer.OrdinalIgnoreCase))
                return NeckSlot;

            if (Spine4Alias.Contains(name, StringComparer.OrdinalIgnoreCase))
                return Spine4Slot;

            if (Spine2Alias.Contains(name, StringComparer.OrdinalIgnoreCase))
                return Spine2Slot;

            if (Spine1Alias.Contains(name, StringComparer.OrdinalIgnoreCase))
                return Spine1Slot;

            if (SpineAlias.Contains(name, StringComparer.OrdinalIgnoreCase))
                return SpineSlot;

            if (PelvisAlias.Contains(name, StringComparer.OrdinalIgnoreCase))
                return PelvisSlot;

            if (LeftClavicleAlias.Contains(name, StringComparer.OrdinalIgnoreCase))
                return LeftClavicleSlot;

            if (LeftUpperArmAlias.Contains(name, StringComparer.OrdinalIgnoreCase))
                return LeftUpperArmSlot;

            if (LeftForearmAlias.Contains(name, StringComparer.OrdinalIgnoreCase))
                return LeftForearmSlot;

            if (LeftHandAlias.Contains(name, StringComparer.OrdinalIgnoreCase))
                return LeftHandSlot;

            if (RightClavicleAlias.Contains(name, StringComparer.OrdinalIgnoreCase))
                return RightClavicleSlot;

            if (RightUpperArmAlias.Contains(name, StringComparer.OrdinalIgnoreCase))
                return RightUpperArmSlot;

            if (RightForearmAlias.Contains(name, StringComparer.OrdinalIgnoreCase))
                return RightForearmSlot;

            if (RightHandAlias.Contains(name, StringComparer.OrdinalIgnoreCase))
                return RightHandSlot;

            if (LeftThighAlias.Contains(name, StringComparer.OrdinalIgnoreCase))
                return LeftThighSlot;

            if (LeftCalfAlias.Contains(name, StringComparer.OrdinalIgnoreCase))
                return LeftCalfSlot;

            if (LeftFootAlias.Contains(name, StringComparer.OrdinalIgnoreCase))
                return LeftFootSlot;

            if (LeftToesAlias.Contains(name, StringComparer.OrdinalIgnoreCase))
                return LeftToesSlot;

            if (RightThighAlias.Contains(name, StringComparer.OrdinalIgnoreCase))
                return RightThighSlot;

            if (RightCalfAlias.Contains(name, StringComparer.OrdinalIgnoreCase))
                return RightCalfSlot;

            if (RightFootAlias.Contains(name, StringComparer.OrdinalIgnoreCase))
                return RightFootSlot;

            if (RightToesAlias.Contains(name, StringComparer.OrdinalIgnoreCase))
                return RightToesSlot;

            return null;
        }

        // Allow accessing by index
        public RigSlot this[int index]
        {
            get
            {
                return index switch
                {
                    0 => HeadSlot,
                    1 => NeckSlot,
                    2 => Spine4Slot,
                    3 => Spine2Slot,
                    4 => Spine1Slot,
                    5 => SpineSlot,
                    6 => PelvisSlot,
                    7 => LeftClavicleSlot,
                    8 => LeftUpperArmSlot,
                    9 => LeftForearmSlot,
                    10 => LeftHandSlot,
                    11 => RightClavicleSlot,
                    12 => RightUpperArmSlot,
                    13 => RightForearmSlot,
                    14 => RightHandSlot,
                    15 => LeftThighSlot,
                    16 => LeftCalfSlot,
                    17 => LeftFootSlot,
                    18 => LeftToesSlot,
                    19 => RightThighSlot,
                    20 => RightCalfSlot,
                    21 => RightFootSlot,
                    22 => RightToesSlot,
                    23 => LeftHandThumb1Slot,
                    24 => LeftHandThumb2Slot,
                    25 => LeftHandThumb3Slot,
                    26 => LeftHandIndex1Slot,
                    27 => LeftHandIndex2Slot,
                    28 => LeftHandIndex3Slot,
                    29 => LeftHandMiddle1Slot,
                    30 => LeftHandMiddle2Slot,
                    31 => LeftHandMiddle3Slot,
                    32 => LeftHandRing1Slot,
                    33 => LeftHandRing2Slot,
                    34 => LeftHandRing3Slot,
                    35 => LeftHandPinky1Slot,
                    36 => LeftHandPinky2Slot,
                    37 => LeftHandPinky3Slot,
                    38 => RightHandThumb1Slot,
                    39 => RightHandThumb2Slot,
                    40 => RightHandThumb3Slot,
                    41 => RightHandIndex1Slot,
                    42 => RightHandIndex2Slot,
                    43 => RightHandIndex3Slot,
                    44 => RightHandMiddle1Slot,
                    45 => RightHandMiddle2Slot,
                    46 => RightHandMiddle3Slot,
                    47 => RightHandRing1Slot,
                    48 => RightHandRing2Slot,
                    49 => RightHandRing3Slot,
                    50 => RightHandPinky1Slot,
                    51 => RightHandPinky2Slot,
                    52 => RightHandPinky3Slot,
                    _ => throw new ArgumentOutOfRangeException(nameof(index), index, "Index must be between 0 and 52")
                };
            }
        }

        public int Count => 53; // Total number of RigSlots defined


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
