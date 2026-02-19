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
        public RigSlot HeadSlot { get; set; } = new RigSlot(                "ValveBiped.Bip01_Head1",           "Head");
        public RigSlot NeckSlot { get; set; } = new RigSlot(                "ValveBiped.Bip01_Neck1",           "Neck");

        public RigSlot Spine4Slot { get; set; } = new RigSlot(              "ValveBiped.Bip01_Spine4",          "Upper Chest");
        public RigSlot Spine2Slot { get; set; } = new RigSlot(              "ValveBiped.Bip01_Spine2",          "Chest");
        public RigSlot Spine1Slot { get; set; } = new RigSlot(              "ValveBiped.Bip01_Spine1",          "Central Spine");
        public RigSlot SpineSlot { get; set; } = new RigSlot(               "ValveBiped.Bip01_Spine",           "Lower Spine");
        public RigSlot PelvisSlot { get; set; } = new RigSlot(              "ValveBiped.Bip01_Pelvis",          "Pelvis");

        public RigSlot LeftClavicleSlot { get; set; } = new RigSlot(        "ValveBiped.Bip01_L_Clavicle",      "Left Clavicle");
        public RigSlot LeftUpperArmSlot { get; set; } = new RigSlot(        "ValveBiped.Bip01_L_UpperArm",      "Left Upper Arm");
        public RigSlot LeftForearmSlot { get; set; } = new RigSlot(         "ValveBiped.Bip01_L_Forearm",       "Left Forearm");
        public RigSlot LeftHandSlot { get; set; } = new RigSlot(            "ValveBiped.Bip01_L_Hand",          "Left Hand");

        public RigSlot RightClavicleSlot { get; set; } = new RigSlot(       "ValveBiped.Bip01_R_Clavicle",      "Right Clavicle");
        public RigSlot RightUpperArmSlot { get; set; } = new RigSlot(       "ValveBiped.Bip01_R_UpperArm",      "Right Upper Arm");
        public RigSlot RightForearmSlot { get; set; } = new RigSlot(        "ValveBiped.Bip01_R_Forearm",       "Right Forearm");
        public RigSlot RightHandSlot { get; set; } = new RigSlot(           "ValveBiped.Bip01_R_Hand",          "Right Hand");

        public RigSlot LeftThighSlot { get; set; } = new RigSlot(           "ValveBiped.Bip01_L_Thigh",         "Left Thigh");
        public RigSlot LeftCalfSlot { get; set; } = new RigSlot(            "ValveBiped.Bip01_L_Calf",          "Left Calf");
        public RigSlot LeftFootSlot { get; set; } = new RigSlot(            "ValveBiped.Bip01_L_Foot",          "Left Foot");
        public RigSlot LeftToesSlot { get; set; } = new RigSlot(            "ValveBiped.Bip01_L_Toe0",          "Left Toes");

        public RigSlot RightThighSlot { get; set; } = new RigSlot(          "ValveBiped.Bip01_R_Thigh",         "Right Thigh");
        public RigSlot RightCalfSlot { get; set; } = new RigSlot(           "ValveBiped.Bip01_R_Calf",          "Right Calf");
        public RigSlot RightFootSlot { get; set; } = new RigSlot(           "ValveBiped.Bip01_R_Foot",          "Right Foot");        
        public RigSlot RightToesSlot { get; set; } = new RigSlot(           "ValveBiped.Bip01_R_Toe0",          "Right Toes");


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
