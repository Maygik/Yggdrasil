
// Model path to compile to
$modelname "@MODEL_PATH@.mdl"

// Bodygroups
@BODYGROUPS@

// Material folders to read from
@MATERIAL_LINES@


// Impact sounds when hit/shot
$surfaceprop "@SURFACE_PROP@"


// Renders transparency in a separate pass. Can generally be left as-is
$mostlyopaque


// Culling box. All 0s means we use the bounding box instead.
$cbox 0 0 0 0 0 0
