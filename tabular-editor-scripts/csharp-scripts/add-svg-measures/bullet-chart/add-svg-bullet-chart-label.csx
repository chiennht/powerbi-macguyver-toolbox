// Script author: Kurt Buhler; Data Goblins
// Script created: Sept 3, 2024
// Script supports: Tabular Editor 2.X, Tabular Editor 3.X.
//
// Original template author: Kurt Buhler
//
//
// Script instructions: Use this script when connected with any Power BI semantic model. Doesn't support AAS models.
//
// 1. Select your measure table - or the table where you want to place the measure - in the TOM Explorer.
// 2. Run the script and validate the resulting DAX. Troubleshoot any possible errors, if necessary.
// 3. Add the measure to a table or matrix visual.
// 4. Set the "Image size" property of the visual to Height: 25 and Width: 100. If you use another size, you might need to adjust the measure DAX.
// 5. Validate the SVG visual in different filter contexts to ensure that it is accurate and performant.


// DAX template
string _SvgString = @"
-- Use this inside of a Table or a Matrix visual.
-- The 'Image size' property of the Table or Matrix should be set to a 'Height' of 25px and a 'Width' of 100px for best results


----------------------------------------------------------------------------------------
-------------------- START CONFIG - SAFELY CHANGE STUFF IN THIS AREA -------------------
----------------------------------------------------------------------------------------


-- Input field config
VAR _Actual = __ACTUAL_MEASURE
VAR _Target = __TARGET_MEASURE
VAR _Performance = DIVIDE ( _Actual - _Target, _Target )


-- Chart Config
VAR _BarMax = 100
VAR _BarMin = 30
VAR _Scope = ALLSELECTED ( __GROUPBY_COLUMN )


-- Color config.
VAR _ActualColor        = ""#cfcfcf"" -- Grey
VAR _BaselineColor      = ""#737373"" -- Dark grey
VAR _BackgroundColor    = ""#f5f5f5"" -- Light grey
VAR _TargetColor        = ""black""   -- Black

VAR _SentimentColor     = 
IF ( 
    _Performance < 0, 
    ""#e4a35f"", -- Blue
    ""#78a1b7""  -- Orange
)


----------------------------------------------------------------------------------------
----------------------- END CONFIG - BEYOND HERE THERE BE DRAGONS ----------------------
----------------------------------------------------------------------------------------


VAR _MaxActualsInScope = 
    CALCULATE(
        MAXX(
            _Scope,
            __ACTUAL_MEASURE
        ),
        REMOVEFILTERS( __GROUPBY_COLUMN )
    )

VAR _MaxTargetInScope = 
    CALCULATE(
        MAXX(
            _Scope,
            __TARGET_MEASURE
        ),
        REMOVEFILTERS( __GROUPBY_COLUMN )
    )

VAR _AxisMax = 
    IF (
        HASONEVALUE ( __GROUPBY_COLUMN ),
        MAX( _MaxActualsInScope, _MaxTargetInScope ),
        CALCULATE( MAX( __ACTUAL_MEASURE, __TARGET_MEASURE ), REMOVEFILTERS( __GROUPBY_COLUMN ) )
    ) * 1.1


-- Normalize values (to get position along X-axis)
    VAR _AxisRange = 
        _BarMax - _BarMin
    
    VAR _ActualNormalized = 
        DIVIDE ( _Actual, _AxisMax ) * _AxisRange

    VAR _TargetNormalized = 
        DIVIDE ( _Target, _AxisMax ) * _AxisRange + _BarMin - 1


-- Vectors and SVG code
VAR _SvgPrefix = ""data:image/svg+xml;utf8, <svg xmlns='http://www.w3.org/2000/svg'>""

VAR _Sort = ""<desc>"" & FORMAT ( _Actual, ""000000000000"" ) & ""</desc>""

VAR _Icon  = ""<text x='0' y='13.5' font-family='Segoe UI' font-size='6' font-weight='700' fill='"" & _SentimentColor & ""'>"" & FORMAT ( _Performance, ""▲;▼;"" ) & ""</text>""
VAR _Label = ""<text x='6.5' y='15' font-family='Segoe UI' font-size='10' font-weight='700' fill='"" & _SentimentColor & ""'>"" & FORMAT ( _Performance, ""#,##0%;#,##0%;#,##0%"" ) & ""</text>""

VAR _BarBaseline = ""<rect x='"" & _BarMin - 1 & ""' y='0' width='1' height='100%' fill='"" & _BaselineColor & ""'/>""
VAR _BarBackground = ""<rect x='"" & _BarMin & ""' y='2' width='"" & _BarMax & ""' height='80%' fill='"" & _BackgroundColor & ""'/>""

VAR _ActualBar  = ""<rect x='"" & _BarMin & ""' y='5' width='"" & _ActualNormalized & ""' height='50%' fill='"" & _ActualColor & ""'/>""
VAR _TargetLine = ""<rect x='"" & _TargetNormalized & ""' y='2' width='1.5' height='80%' fill='"" & _TargetColor & ""'/>""

VAR _SvgSuffix = ""</svg>""


-- Final result
VAR _SVG = 
    _SvgPrefix 
    
    & _Sort

    & _BarBackground

    & _Icon 
    & _Label 

    & _ActualBar 
    & _TargetLine 

    & _BarBaseline 

    & _SvgSuffix

RETURN
    _SVG
";


// Selected values you want to use in the plot.
var _AllMeasures = Model.AllMeasures.Where(m => m.IsHidden != true).OrderBy(m => m.Name);
var _AllColumns = Model.AllColumns.Where(c => c.IsHidden != true).OrderBy(c => c.DaxObjectFullName);

var _Actual = SelectMeasure(_AllMeasures, null,"Select the measure that you want to measure:");
var _Target = SelectMeasure(_AllMeasures, null,"Select the measure that you want to compare to:");
var _GroupBy = SelectColumn(_AllColumns, null, "Select the column for which you will group the data in\nthe table or matrix visual:");

_SvgString = _SvgString.Replace( "__ACTUAL_MEASURE", _Actual.DaxObjectFullName ).Replace( "__TARGET_MEASURE", _Target.DaxObjectFullName ).Replace( "__GROUPBY_COLUMN", _GroupBy.DaxObjectFullName );

// Adding the measure.
var _SelectedTable = Selected.Table;
string _Name = "SVG Bullet Chart (with Label)";
string _Desc = _Name + " of " + _Actual.Name + " vs. " + _Target.Name + ", grouped by " + _GroupBy.Name;
var _SvgMeasure = _SelectedTable.AddMeasure( "New " + _Name, _SvgString, "SVGs\\Bullet Chart");

// Setting measure properties.
_SvgMeasure.DataCategory = "ImageUrl";
_SvgMeasure.IsHidden = true;
_SvgMeasure.Description = _Desc;

// Notification InfoBox.
Info("Added new SVG measure to the table " + _SelectedTable.Name + ".\n\nValidate the SVG specification and test the measure carefully in many different filter contexts before using it in reports.\nDon't forget to rename the new measure.");