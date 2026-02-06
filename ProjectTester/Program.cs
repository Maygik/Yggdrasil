
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Yggdrassil.Application.UseCases;
using Yggdrassil.Domain.Project;
using Yggdrassil.Domain.QC;

var project = new Project
{
    Name = "TestProject",
    Qc = new QcConfig
    {
        ModelPath = "myaddon/test_model",
        CdMaterialsPaths = new List<string> { "myaddon/materials", "myaddon/other_materials" },
        AnimationProfile = AnimationProfile.MalePlayer,
        Features = new QcFeatures
        {
            UseIk = true,
            UseHitboxes = true,
            UseRagdoll = false,
        },
        Bodygroups = new List<Bodygroup>
        {
            new Bodygroup("Body", new List<string> { "body", "body2" }),
            new Bodygroup("Head", new List<string> { "head", "headwithhat" }),

        },
        IllumBone = "ValveBiped.Bip01_Pelvis",
        SurfaceProp = "flesh",
    },
    Build = new BuildSettings
    {
        OutputDirectory = "output"
    }
};

JSonIO.Save("test_project.json", project);

Console.WriteLine("Project file 'test_project.json' has been created.");
