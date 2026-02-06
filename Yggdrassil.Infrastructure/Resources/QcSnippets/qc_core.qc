
// Model path to compile to
$modelname "@MODEL_PATH@.mdl"

// Bodygroups
@BODYGROUPS@

// Material folders to read from
@MATERIAL_LINES@


// Impact sounds when hit/shot
$surfaceprop "@SURFACE_PROP@"


// Calculate lighting a little above the pelvis. Roughly in the middle.
$illumposition 0 0 10 "@ILLUM_POSITION@"


// Renders transparency in a separate pass. Can generally be left as-is
$mostlyopaque


// Culling box. All 0s means we use the bounding box instead.
$cbox 0 0 0 0 0 0
