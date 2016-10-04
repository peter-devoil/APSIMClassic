using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using System.Linq.Expressions;
using System.Xml;
using System.Xml.Schema;
using ModelFramework;
using CSGeneral;


/// <summary>
/// A multi-species pasture model
/// </summary>
public class AgPasture
{
    #region Links, events and delegates  -----------------------------------------------------------------------------------

    ////- Links >>> - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -

    [Link]
    private Component My = null;

    [Link]
    private Clock myClock;

    [Link]
    private MetFile MetData;

    ////- Events >>>  - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -

    /// <summary>NewCrop event</summary>
    [Event]
    public event NewCropDelegate NewCrop;

    /// <summary>New_Canopy event</summary>
    [Event]
    public event NewCanopyDelegate New_Canopy;

    /// <summary>NewPotentialGrowth event</summary>
    [Event]
    public event NewPotentialGrowthDelegate NewPotentialGrowth;

    /// <summary>IncorpFOM event</summary>
    [Event]
    public event FOMLayerDelegate IncorpFOM;

    /// <summary>BiomassRemoved event</summary>
    [Event]
    public event BiomassRemovedDelegate BiomassRemoved;

    /// <summary>WaterChanged event</summary>
    [Event]
    public event WaterChangedDelegate WaterChanged;

    /// <summary>NitrogenChanged event</summary>
    [Event]
    public event NitrogenChangedDelegate NitrogenChanged;

    /// <summary>NitrogenChanged event</summary>
    [Event]
    public event AddSoilCNPatchDelegate AddSoilCNPatch;

    #endregion  ------------------------------------------------------------------------------------------------------------

    #region Model parameters  ----------------------------------------------------------------------------------------------

    ////- General parameters (for sward or initialisation only) >>> - - - - - - - - - - - - - - - - - - - - - - - - - -

    [Description("Name of the sward mix")]
    private string thisCropName = "";

    [Param]
    [Description("Name of species to simulate")]
    [Units("")]
    private string[] speciesToSimulate = null;

    [Param]
    [Description("Whether water uptake is calculated by agpasture or apsim")]
    [Units("calc/apsim")]
    private string WaterUptakeSource = "calc";

    [Param]
    [Description("Whether N uptake is calculated by agpasture or apsim")]
    [Units("calc/apsim")]
    private string NUptakeSource = "calc";

    private bool usingSpeciesRoot = false;

    [Param]
    [Description("Whether root distribution is determined for each species, instead of avg sward")]
    [Units("yes/no")]
    private string UseRootBySpecies
    {
        get
        {
            if (usingSpeciesRoot)
                return "yes";
            else
                return "no";
        }
        set { usingSpeciesRoot = value.ToLower() == "yes"; }
    }

    private bool usingSpeciesPhotosynthesis = false;

    [Param]
    [Description("Whether photosynthesis is computed by species or average sward")]
    [Units("yes/no")]
    private string UsePhotosynthesisBySpecies
    {
        get
        {
            if (usingSpeciesPhotosynthesis)
                return "yes";
            else
                return "no";
        }
        set { usingSpeciesPhotosynthesis = value.ToLower() == "yes"; }
    }

    private bool updateLightExtCoeffAllowed = false;

    [Param]
    [Description("Whether the light extinction coefficient of whole sward is computed every day")]
    [Units("yes/no")]
    private string UpdateLightExtCoeffDaily
    {
        get
        {
            if (updateLightExtCoeffAllowed)
                return "yes";
            else
                return "no";
        }
        set { updateLightExtCoeffAllowed = value.ToLower() == "yes"; }
    }

    private bool usingWAvailableBySpecies = false;

    [Param]
    [Description("Whether the water availability is determined by species, instead of whole sward")]
    [Units("yes/no")]
    private string UseWaterAvailableBySpecies
    {
        get
        {
            if (usingWAvailableBySpecies)
                return "yes";
            else
                return "no";
        }
        set { usingWAvailableBySpecies = value.ToLower() == "yes"; }
    }

    private int waterExtractabilityMethod = 0;

    [Param]
    [Description("The index marking the method used for determining plant available water")]
    [Units("0-3")]
    private double WaterAvailabilityMethod
    {
        get { return waterExtractabilityMethod; }
        set { waterExtractabilityMethod = (int) value; }
    }

    private bool usingWUptakeBySpecies = false;

    [Param]
    [Description("Whether water uptake is determined by species, instead of whole sward")]
    [Units("yes/no")]
    private string UseWaterUptakeBySpecies
    {
        get
        {
            if (usingWUptakeBySpecies)
                return "yes";
            else
                return "no";
        }
        set { usingWUptakeBySpecies = value.ToLower() == "yes"; }
    }

    private bool usingNAvailableBySpecies = false;

    [Param]
    [Description("Whether the N availability is determined by species, instead of whole sward")]
    [Units("yes/no")]
    private string UseNAvailableBySpecies
    {
        get
        {
            if (usingNAvailableBySpecies)
                return "yes";
            else
                return "no";
        }
        set { usingNAvailableBySpecies = value.ToLower() == "yes"; }
    }

    private int NExtractabilityMethod = 0;

    [Param]
    [Description("The index marking the method used for determining plant available N")]
    [Units("0-3")]
    private double NAvailabilityMethod
    {
        get { return NExtractabilityMethod; }
        set { NExtractabilityMethod = (int) value; }
    }

    private bool usingAlternativeNUptake = false;

    [Param]
    [Description("Whether alternative method for determining plant N uptake is to be used")]
    [Units("yes/no")]
    private string UseAlternativeNUptake
    {
        get
        {
            if (usingAlternativeNUptake)
                return "yes";
            else
                return "no";
        }
        set { usingAlternativeNUptake = value.ToLower() == "yes"; }
    }

    ////- Initial state parameters (replace the default values) >>> - - - - - - - - - - - - - - - - - - - - - - - - - -

    [Param(IsOptional = true)]
    [Description("Initial above ground DM weight for each species")]
    [Units("kg/ha")]
    private double[] iniShootDM;

    [Param(IsOptional = true)]
    [Description("Initial below ground DM weight for each species")]
    [Units("kg/ha")]
    private double[] iniRootDM;

    [Param(IsOptional = true)]
    [Description("Initial rooting depth for each species")]
    [Units("kg/ha")]
    private double[] iniRootDepth;

    [Param(IsOptional = true)]
    private double[] iniRootDepthParam = null;

    [Param(IsOptional = true)]
    private double[] iniRootCurveParam = null;

    #region - Values for each parameterised pasture species - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -

    ////- General parameters (name and type) >>>  - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
    [Param]
    [Description("Actual name of each species")]
    [Units("")]
    private string[] speciesName = null;

    [Param]
    [Description("Plant type for micromet")]
    [Units("")]
    private string[] micrometType = null;

    [Param]
    [Description("Metabolic pathway for C fixation during photosynthesis (C3 or C4)")]
    [Units("3/4")]
    private double[] photoPath = null;

    [Param]
    [Description("Whether the plant is a legume species (1=yes, 0=no)")]
    [Units("0/1")]
    private double[] isLegume = null;

    //[Param]
    [Description("Whether the plant is an annual species (1=yes, 0=no)")]
    [Units("0/1")]
    private double[] isAnnual = null;

    ////- Potential growth (photosynthesis) >>> - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
    [Param]
    [Description("Reference leaf CO2 assimilation rate for photosynthesis")]
    [Units("mgCO2/m^2 leaf/s")]
    private double[] Pm;

    [Param]
    [Description("Relative factor for light partition between species")]
    [Units("-")]
    private double[] LightPartitioningFactor;

    [Param]
    [Description("Leaf photosynthetic efficiency")]
    [Units("mg CO2/J")]
    private double[] alphaPhoto;

    [Param]
    [Description("Photosynthesis curvature parameter")]
    [Units("J/kg/s")]
    private double[] thetaPhoto;

    [Param]
    [Description("Fraction of radiation that is photosynthetic active")]
    [Units("0-1")]
    private double[] fractionPAR;

    [Param]
    [Description("Light extinction coefficient")]
    [Units("")]
    private double[] lightExtCoeff;

    [Param]
    [Description("Reference CO2 concentration for photosynthesis")]
    [Units("ppm")]
    private double[] referenceCO2;

    [Param]
    [Description("Scaling parameter for the CO2 effect on photosynthesis")]
    [Units("ppm")]
    private double[] CO2PmaxScale;

    [Param]
    [Description("Scaling parameter for the CO2 effects on N requirements")]
    [Units("ppm")]
    private double[] CO2NScale;

    [Param]
    [Description("Minimum value for the CO2 effect on N requirements")]
    [Units("0-1")]
    private double[] CO2NMin;

    [Param]
    [Description("Exponent controlling the CO2 effect on N requirements")]
    [Units(">1.0")]
    private double[] CO2NCurvature;

    [Param]
    [Description("Minimum temperature for growth")]
    [Units("")]
    private double[] growthTmin;

    [Param]
    [Description("Optimum temperature for growth")]
    [Units("")]
    private double[] growthTopt;

    [Param]
    [Description("Curve parameter for growth response to temperature")]
    [Units("")]
    private double[] growthTq;

    [Param]
    [Description("Whether heat damage stress is enabled")]
    [Units("")]
    private string[] useHeatStress;

    [Param]
    [Description("Onset temperature for heat effects on photosynthesis")]
    [Units("")]
    private double[] heatOnsetT;

    [Param]
    [Description("Temperature for full heat effect on photosynthesis")]
    [Units("")]
    private double[] heatFullT;

    [Param]
    [Description("Cumulative degrees-day for recovery from heat stress")]
    [Units("")]
    private double[] heatSumT;

    [Param]
    [Description("Reference temperature for recovery from heat stress")]
    [Units("oC")]
    private double[] heatRecoverT;

    [Param]
    [Description("Whether cold damage stress is enabled")]
    [Units("")]
    private string[] useColdStress;

    [Param]
    [Description("Onset temperature for cold effects on photosynthesis")]
    [Units("")]
    private double[] coldOnsetT;

    [Param]
    [Description("Temperature for full cold effect on photosynthesis")]
    [Units("")]
    private double[] coldFullT;

    [Param]
    [Description("Cumulative degrees for recovery from cold stress")]
    [Units("")]
    private double[] coldSumT;

    [Param]
    [Description("Reference temperature for recovery from cold stress")]
    [Units("oC")]
    private double[] coldRecoverT;

    ////- Respiration parameters >>>  - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
    [Param]
    [Description("Maintenance respiration coefficient")]
    [Units("%")]
    private double[] maintRespiration;

    [Param]
    [Description("Growth respiration coefficient")]
    [Units("0-1")]
    private double[] growthEfficiency;

    [Param]
    [Description("Reference temperature for maintenance respiration")]
    [Units("oC")]
    private double[] respTref;

    [Param]
    [Description("Exponent controlling the effect of temperature on respiration")]
    [Units("")]
    private double[] respExponent;

    ////- N concentrations thresholds >>> - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
    [Param]
    [Description("Optimum N concentration of leaves (no stress)")]
    [Units("%")]
    private double[] NconcOptimum_leaves;

    [Param]
    [Description("Maximum N concentration of leaves (luxury uptake)")]
    [Units("%")]
    private double[] NconcMaximum_leaves;

    [Param]
    [Description("Minimum N concentration of leaves (dead leaves)")]
    [Units("%")]
    private double[] NconcMinimum_leaves;

    [Param]
    [Description("N concentration for stems, relative to leaves")]
    [Units("0-1")]
    private double[] RelativeNconc_Stems;

    [Param]
    [Description("N concentration for stolons, relative to leaves")]
    [Units("0-1")]
    private double[] RelativeNconc_Stolons;

    [Param]
    [Description("N concentration for roots, relative to leaves")]
    [Units("0-1")]
    private double[] RelativeNconc_Roots;

    [Param]
    [Description("N concentration for plants at stage 2 (developing), relative to optimum")]
    [Units("0-1")]
    private double[] RelativeNconc_stage2;

    [Param]
    [Description("N concentration for plants at stage 3 (mature), relative to optimum")]
    [Units("0-1")]
    private double[] RelativeNconc_stage3;

    ////- Default values for DM >>> - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
    [Param]
    [Description("Default shoot DM weight")]
    [Units("kgDM/ha")]
    private double[] dmshoot;

    [Param]
    [Description("default root DM weight")]
    [Units("kgDM/ha")]
    private double[] dmroot;

    [Param]
    [Description("Default rooting depth")]
    [Units("mm")]
    private double[] rootDepth;

    [Param]
    [Description("Initial fractions of DM for each plant part, for non-legumes")]
    [Units("0-1")]
    private double[] initialDMFractions_grass;

    [Param]
    [Description("Initial fractions of DM for each plant part, for legume species")]
    [Units("0-1")]
    private double[] initialDMFractions_legume;

    ////- Germination and emergence >>> - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
    [Param]
    [Description("Cumulative degrees-day needed for seed germination")]
    [Units("oCd")]
    private double[] DegreesDayForGermination { get; set; }

    [Param]
    [Description("The fractions of DM for each plant part at emergence, for all plants")]
    [Units("0-1")]
    private double[] EmergenceDMFractions;

    ////- Allocation of new growth >>>  - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
    // - Shoot:root partition
    [Param]
    [Description("Target or ideal plant's shoot:root ratio at vegetative stage")]
    [Units("-")]
    private double[] TargetSRratio;

    [Param]
    [Description("Maximum fraction of DM growth that can be allocated to roots")]
    [Units("0-1")]
    private double[] MaxRootAllocation;

    [Param]
    [Description("Maximum effect that soil GLFs have on Shoot-Root ratio")]
    [Units("0-1")]
    private double[] GlfEffectsOnSR;

    // - Effect of reproductive season ....................................
    [Param]
    [Description("Whether Shoot:Root ratio should be adjusted to mimic DM allocation during reproductive season (perennial species)")]
    [Units("yes/no")]
    private string[] UseReproSeasonFactor;

    [Param]
    [Description("Reference latitude determining timing for reproductive season")]
    [Units("degrees")]
    private double[] ReproSeasonReferenceLatitude;

    [Param]
    [Description("Coefficient controlling the time to start the reproductive season as function of latitude")]
    [Units("-")]
    private double[] ReproSeasonTimingCoeff;

    [Param]
    [Description("Coefficient controlling the duration of the reproductive season as function of latitude")]
    [Units("-")]
    private double[] ReproSeasonDurationCoeff;

    [Param]
    [Description("Ratio between the length of shoulders and the period with full reproductive growth effect")]
    [Units("-")]
    private double[] ReproSeasonShouldersLengthFactor;

    [Param]
    [Description("The proportion of the length of shoulder before the period with full reproductive growth effect")]
    [Units("0-1")]
    private double[] ReproSeasonOnsetDurationFactor;

    [Param]
    [Description("Maximum increase in DM allocation to shoot during reproductive growth")]
    [Units("0-1")]
    private double[] ReproSeasonMaxAllocationIncrease;

    [Param]
    [Description("Coefficient controlling the increase in shoot allocation during reproductive growth as function of latitude")]
    [Units("-")]
    private double[] ReproSeasonAllocationCoeff;

    // - Partition of shoot DM into leaves
    [Param]
    [Description("Maximum target allocation of new growth to leaves")]
    [Units("0-1")]
    private double[] FractionLeafMaximum;

    [Param]
    [Description("Minimum target allocation of new growth to leaves")]
    [Units("0-1")]
    private double[] FractionLeafMinimum;

    [Param]
    [Description("Shoot DM at which allocation of new growth to leaves start to decrease")]
    [Units("kgDM/ha")]
    private double[] FractionLeafDMThreshold;

    [Param]
    [Description("Shoot DM when allocation to leaves is midway maximum and minimum")]
    [Units("kgDM/ha")]
    private double[] FractionLeafDMFactor;

    [Param]
    [Description("Exponent of function describing DM allocation to leaves")]
    [Units(">0.0")]
    private double[] FractionLeafExponent;

    [Param]
    [Description("Fraction of new growth to be allocated to stolon")]
    [Units("0-1")]
    private double[] StolonAllocationFactor;

    // - Conversion of DM in organ
    [Param]
    [Description("Specific leaf area, per dry matter weight")]
    [Units("m^2/kgDM")]
    private double[] SpecificLeafArea;

    [Param]
    [Description("Specific root length, per dry matter weight")]
    [Units("m/gDM")]
    private double[] SpecificRootLength;

    [Param]
    [Description("Fraction of stolon tissue used when computing green LAI")]
    [Units("0-1")]
    private double[] StolonEffectOnLAI;

    [Param]
    [Description("Maximum aboveground biomass for using stems when computing LAI")]
    [Units("kg/ha")]
    private double[] ShootMaxEffectOnLAI;

    [Param]
    [Description("Maximum effect of stems when computing green LAI")]
    [Units("0-1")]
    private double[] MaxStemEffectOnLAI;

    ////- Tissue turnover and senescence >>>  - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
    [Param]
    [Description("Number of live leaves per tiller")]
    [Units("")]
    private double[] LiveLeavesPerTiller;

    [Param]
    [Description("Reference daily DM turnover rate for shoot tissues")]
    [Units("")]
    private double[] rateLive2Dead;

    [Param]
    [Description("Reference daily DM turnover rate for root tissues")]
    [Units("")]
    private double[] rateRootSen;

    [Param]
    [Description("Relative turnover rate for emerging tissues")]
    [Units("<0.0")]
    private double[] facGrowingTissue;

    [Param]
    [Description("Reference daily detachment rate for dead tissues")]
    [Units("")]
    private double[] rateDead2Litter;

    [Param]
    [Description("Minimum temperature for tissue turnover")]
    [Units("")]
    private double[] massFluxTmin;

    [Param]
    [Description("Reference temperature for tissue turnover")]
    [Units("")]
    private double[] massFluxTopt;

    [Param]
    [Description("Exponent of function for temperature effect on tissue turnover")]
    [Units("")]
    private double[] massFluxTq;

    [Param(MinVal = 1.0)]
    [Description("Maximum increase in tissue turnover due to water deficit")]
    [Units("")]
    private double[] massFluxW0;

    [Param]
    [Description("Minimum GLFwater without effect on tissue turnover")]
    [Units("")]
    private double[] massFluxWopt;

    [Param]
    [Description("Coefficient controlling detachment rate as function of moisture")]
    [Units("")]
    private double[] massFluxDeadWq;

    [Param]
    [Description("Minimum effect of drought on detachment rate")]
    [Units("")]
    private double[] massFluxDeadWf;

    [Param]
    [Description("Factor increasing tissue turnover rate due to stock trampling")]
    [Units("")]
    private double[] stockParameter;

    [Param]
    [Description("Coefficient of function increasing the stolons turnover rate due to defoliation")]
    [Units("")]
    private double[] massFluxDefoliationEffect;

    [Param]
    [Description("Minimum significant daily effect of defoliation on stolons turnover rate")]
    [Units("")]
    private double[] massFluxMinDefoliationEff;

    [Param]
    [Description("Fraction of luxury N remobilisable from tissue 2 each day")]
    [Units("0-1")]
    private double[] Kappa2_Remob;

    [Param]
    [Description("Fraction of luxury N remobilisable from tissue 3 each day")]
    [Units("0-1")]
    private double[] Kappa3_Remob;

    [Param]
    [Description("Coefficient for partitioning non-used Nremob into tissue 4")]
    [Units("0-1")]
    private double[] Kappa4_Remob;

    ////- N fixation (for legumes) >>>  - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
    [Param]
    [Description("Minimum fraction of N demand supplied by biologic N fixation")]
    [Units("0-1")]
    private double[] NMinFix;

    [Param]
    [Description("Maximum fraction of N demand supplied by biologic N fixation")]
    [Units("0-1")]
    private double[] NMaxFix;

    private int NFixationCostMethod = 0;

    [Param]
    [Description("Which method is used for determining the costs of N fixation")]
    [Units("0-2")]
    private double NFixCostMethod
    {
        get { return NFixationCostMethod; }
        set { NFixationCostMethod = (int) value; }
    }

    [Param]
    [Description("Respiration cost factor due to the presence of symbiont bacteria")]
    [Units("kgC/kgCroots")]
    private double[] symbiontCostFactor;

    [Param]
    [Description("Respiration cost factor due to the activity of symbiont bacteria")]
    [Units("kgC/kgNfixed")]
    private double[] NFixingCostFactor;

    ////- Growth limiting factors >>> - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
    [Param]
    [Description("Maximum reduction in plant growth due to water logging (saturated soil)")]
    [Units("0-1")]
    private double[] soilSatFactor;

    [Param]
    [Description("Minimum water-free pore space for growth with no limitations")]
    [Units("0-1")]
    private double[] MinMacroPorosity;

    [Param]
    [Description("Daily recovery rate from water logging")]
    [Units("0-1")]
    private double[] SoilSaturationRecoveryFactor;

    [Param]
    [Description("Exponent for modifying the effect of N deficiency on plant growth")]
    [Units("")]
    private double[] NdilutCoeff;

    [Param]
    [Description("Generic factor affecting potential plant growth")]
    [Units("0-1")]
    private double[] GenericGLF;

    [Param]
    [Description("Generic growth limiting factor due to soil fertility")]
    [Units("0-1")]
    private double[] SFertilityGLF;

    ////- Plant height >>>  - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
    [Param]
    [Description("Minimum plant height, for each species in the sward")]
    [Units("mm")]
    private double[] MinimumHeight;

    [Param]
    [Description("Maximum plant height, for each species in the sward")]
    [Units("mm")]
    private double[] MaxPlantHeight;

    [Param]
    [Description("DM weight above ground for maximum plant height")]
    [Units("kgDM/ha")]
    private double[] MassForMaxHeight;

    [Param]
    [Description("Exponent controlling shoot height as function of DM weight")]
    [Units(">1.0")]
    private double[] ExponentHeightFromMass;

    ////- Root depth and distribution >>> - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
    [Param]
    [Description("Minimum rooting depth, at emergence")]
    [Units("mm")]
    private double[] MinimumRootDepth { get; set; }

    //[Param]
    [Description("Maximum rooting depth")]
    [Units("mm")]
    private double[] maxRootDepth; // This is being set as iniRootDepth

    [Param]
    [Description("Daily root elongation rate at optimum temperature")]
    [Units("mm/day")]
    private double[] RootElongationRate { get; set; }

    private double[] rootTopDepthParam;

    [Param]
    [Output]
    [Description("Depth from surface where root proportion starts to decrease")]
    [Units("mm")]
    private double[] ExpoLinearDepthParam
    {
        get { return rootTopDepthParam; }
        set
        {
            rootTopDepthParam = new double[value.Length];
            for (int s = 0; s < value.Length; s++)
                rootTopDepthParam[s] = value[s];
        }
    }

    private double[] rootCurveParam;

    [Param]
    [Output]
    [Description("Exponent controlling the root distribution as function of depth")]
    [Units("")]
    private double[] ExpoLinearCurveParam
    {
        get { return rootCurveParam; }
        set
        {
            rootCurveParam = new double[value.Length];
            for (int s = 0; s < value.Length; s++)
                rootCurveParam[s] = value[s];
        }
    }

    ////- Digestibility and feed quality >>>  - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
    [Param]
    [Description("Digestibility of cell wall in live plant tissues (0-1)")]
    [Units("")]
    private double[] digestLiveCellWall;

    [Param]
    [Description("Digestibility of cell wall in dead plant tissues (0-1)")]
    [Units("")]
    private double[] digestDeadCellWall;

    [Param]
    [Description("Fraction of soluble carbohydrates in newly grown tissues (0-1)")]
    [Units("0-1")]
    private double[] SugarFractionNewGrowth;

    ////- Harvest limits and preferences >>>  - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
    [Param]
    [Output]
    [Description("Minimum above ground green DM")]
    [Units("kgDM/ha")]
    private double[] dmgreenmin;

    [Param]
    [Description("Proportion of stolon DM standing, available for removal")]
    [Units("0-1")]
    private double[] FractionStolonsStanding;

    [Param]
    [Description("Relative preference for live over dead material during graze")]
    [Units("")]
    private double[] PreferenceForGreenDM;

    [Param]
    [Description("Relative preference for leaf over stem-stolon material during graze")]
    [Units("")]
    private double[] PreferenceForLeaves;

    ////- Soil related (water and N uptake) >>> - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
    [Param]
    [Description("Maximum NH4 uptake rate for each species")]
    [Units("ppm/day")]
    private double[] MaximumUptakeRateNH4;

    [Param]
    [Description("Maximum NO3 uptake rate for each species")]
    [Units("ppm/day")]
    private double[] MaximumUptakeRateNO3;

    private double[] refRLD;

    [Param]
    [Description("Reference root length density for water and N uptake")]
    [Units("cm/cm3")]
    private double[] referenceRLD
    {
        get { return refRLD; }
        set
        {
            // convert values to mm/mm3
            refRLD = new double[value.Length];
            for (int s = 0; s < value.Length; s++)
                refRLD[s] = value[s] * 0.01;
        }
    }

    [Param]
    [Description("Reference hydraulic conductivity for water and N uptake")]
    [Units("mm/day")]
    private double[] ReferenceKSuptake;

    [Param]
    [Description("Exponent of water content factor for water and N uptake")]
    [Units("-")]
    private double[] ExponentSWCUptake;

    [Param]
    [Description("Coefficient for NH4 availability, for each layer")]
    [Units("0-1")]
    private double[] kNH4;

    [Param]
    [Description("Coefficient for NO3 availability, for each layer")]
    [Units("0-1")]
    private double[] kNO3;

    // - Passed on by the soil module
    [Param]
    [Description("SW uptake parameter (/day)")]
    [Units("0-1")]
    private double[] kl = null;

    [Param]
    [Description("Crop Lower Limit for water uptake (mm/mm)")]
    [Units("mm3/mm3")]
    private double[] ll = null;

    [Param]
    [Description("Exploration factor, for each soil layer - affects root growth")]
    [Units("0-1")]
    private double[] xf = null;

    ////- Parameters for annual species >>> - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -

    //  NOTE: these were de-activated (hidden) as they are not really used and some procedure were
    //   never really implemented  (RCichota, Oct/2014)
    //[Param]
    [Description("Earliest day of emergence (for annuals only)")]
    [Units("")]
    private double[] dayEmerg;

    //[Param]
    [Description("Earliest month of emergence (for annuals only)")]
    [Units("")]
    private double[] monEmerg;

    //[Param]
    [Description("Earliest day of anthesis (for annuals only)")]
    [Units("")]
    private double[] dayAnth;

    //[Param]
    [Description("Earliest month of anthesis (for annuals only)")]
    [Units("")]
    private double[] monAnth;

    //[Param]
    [Description("Days from anthesis to maturity (for annuals only)")]
    [Units("")]
    private double[] daysToMature;

    ////- Additional functions (vapour pressure deficit) >>>  - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
    [Link]
    private LinearInterpolation FVPDFunction = null;

    #endregion  - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -

    #endregion  ------------------------------------------------------------------------------------------------------------

    #region Inputs from other modules  -------------------------------------------------------------------------------------

    [Input]
    private double[] dlayer; //Soil Layer Thickness (mm)

    [Input]
    private double[] sw_dep; //soil water by layer

    [Input]
    private double[] SAT_dep; //saturation point

    [Input]
    private double[] DUL_dep; //drainage upper limit (field capacity);

    [Input]
    private double[] LL15_dep; //drainage lower limit (wilting point);

    [Input]
    private double[] ks; //saturated hydraulic conductivity;

    [Input]
    private double[] nh4; //SNH4dep = new float[dlayer.Length];

    [Input]
    private double[] no3; //SNO3dep = new float[dlayer.Length];

    [Input]
    private double[] bd; // soil bulk density

    // - Making AgPasture patch-aware  ---------------------------------------------
    [Input(IsOptional = true)]
    private double[] nh4_PlantAvailable;

    [Input(IsOptional = true)]
    private double[] no3_PlantAvailable;

    [Input(IsOptional = true)]
    private double[] PatchArea;

    [Input(IsOptional = true)]
    private CNPatchVariableType PatchNH4;

    [Input(IsOptional = true)]
    private CNPatchVariableType PatchNO3;

    // -----------------------------------------------------------------------------

    /// <summary>CO2 in atmosphere</summary>
    [Input]
    [Description("Actual CO2, updated from met and ClimateControl")]
    [Units("")]
    public double co2 = 380;

    /// <summary>
    /// Gets or sets the effective stocking rate, to calculate trampling on pasture (increase senescence)
    /// </summary>
    public double StockRate
    {
        get { return Species.stockingRate; }
        set { Species.stockingRate = value; }
    }

    #endregion  ------------------------------------------------------------------------------------------------------------

    #region Private variables  ---------------------------------------------------------------------------------------------

    ////- General variables >>> - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -

    /// <summary>The collection of pasture species in the sward</summary>
    private Species[] mySpecies;

    /// <summary>Number of species in the sward</summary>
    private int NumSpecies = 0;

    ////- Plant growth and turnover >>> - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -

    /// <summary>Sward average light extinction coefficient</summary>
    private double swardLightExtCoeff;

    /// <summary>Daily potential growth (kg/ha)</summary>
    private double swardNetPotentialGrowth;

    /// <summary>Daily potential growth, after water stress (kg/ha)</summary>
    private double swardPotGrowthAfterWater;

    /// <summary>Daily actual growth (kg/ha)</summary>
    private double swardPotGrowthAfterNutrient;

    /// <summary>Daily litter formation (kg/ha)</summary>
    private double swardLitterDM;

    /// <summary>Daily root senescence (kg/ha)</summary>
    private double swardSenescedRootDM;

    /// <summary>Amount of N in litter (kgN/ha)</summary>
    private double swardLitterN;

    /// <summary>Amount of N in root senesced (kgN/ha)</summary>
    private double swardSenescedRootN;

    ////- Water uptake >>>  - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -

    /// <summary>Plant available soil water for each layer (mm)</summary>
    private double[] soilAvailableWater;

    /// <summary>Daily total soil water demand (mm)</summary>
    private double swardWaterDemand;

    /// <summary>Soil water taken up from each layer (mm)</summary>
    private double[] soilWaterUptake;

    /// <summary>Soil water uptake as given by an external module (mm)</summary>
    private double[] swardWaterUptakeByAPSIM;

    /// <summary>Lower limit for soil water uptake (whole sward, from soil)</summary>
    private double[] LL_dep;

    ////- N demand and uptake >>> - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -

    /// <summary>Amount of N demanded with luxury uptake</summary>
    private double swardNdemandLux = 0.0;

    /// <summary>Amount of N demanded for optimum growth</summary>
    private double swardNdemandOpt = 0.0;

    /// <summary>Amount of N fixed by legumes</summary>
    private double swardNFixed = 0.0;

    /// <summary>Soil NH4_N uptake as given by an external module (kgN/ha)</summary>
    private double[] swardNH4UptakeByAPSIM;

    /// <summary>Soil NO3_N uptake as given by an external module (kgN/ha)</summary>
    private double[] swardNO3UptakeByAPSIM;

    /// <summary>Amount of N available in each soil layer (kgN/ha)</summary>
    private double[] soilNH4Available;

    /// <summary>Amount of N available in each soil layer (kgN/ha)</summary>
    private double[] soilNO3Available;

    /// <summary>Total plant available N in soil (kgN/ha)</summary>
    private double swardSoilNavailable;

    /// <summary>Plant N demand from soil (kgN/ha)</summary>
    private double swardSoilNDemand;

    /// <summary>Amount of N taken up from each soil layer (kgN/ha)</summary>
    private double[] soilNH4Uptake;

    /// <summary>Amount of N taken up from each soil layer (kgN/ha)</summary>
    private double[] soilNO3Uptake;

    ////- Growth limiting factors >>> - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -

    /// <summary>Growth limiting factor due to ambient temperature</summary>
    private double swardGLFTemp = 1.0;

    /// <summary>Growth limiting factor due to soil nitrogen</summary>
    private double swardGLFN = 1.0;

    /// <summary>Growth limiting factor due to soil water</summary>
    private double swardGLFWater = 1.0;

    /// <summary>Growth limiting factor due to soil aeration</summary>
    private double swardGLFWLogging = 1.0;

    ////- Root depth and distribution >>> - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -

    /// <summary>sward average root zone depth (mm)</summary>
    private double swardRootDepth = 0.0;

    /// <summary>Soil layer at bottom of root zone</summary>
    private int swardRootZoneBottomLayer;

    /// <summary>The ideal fraction of roots DM in each layer</summary>
    private double[] swardTargetRootAllocation;

    /// <summary>The current fraction of roots DM in each layer</summary>
    private double[] swardRootFraction;

    ////- Harvest variables >>> - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -

    /// <summary>Amount of DM harvested</summary>
    private double swardHarvestedDM;

    /// <summary>Amount of N harvested</summary>
    private double swardHarvestedN;

    /// <summary>Digestibility of harvested material</summary>
    private double swardHarvestDigestibility;

    /// <summary>Fraction to be harvest from each species</summary>
    private double[] FractionToHarvest;

    #endregion  ------------------------------------------------------------------------------------------------------------

    #region Constants  -----------------------------------------------------------------------------------------------------

    /// <summary>C fraction on DM, for conversion</summary>
    const double CarbonFractionDM = 0.4;

    /// <summary>Average potential ME concentration in herbage material (MJ/kg)</summary>
    const double PotentialMEOfHerbage = 16.0;

    /// <summary>Minimum significant difference between two values</summary>
    const double Epsilon = 0.000000001;

    #endregion  ------------------------------------------------------------------------------------------------------------

    #region Initialisation methods  ----------------------------------------------------------------------------------------

    /// <summary>EventHandler - initialisation</summary>
    [EventHandler]
    public void OnInit2()
    {
        // Initial parameters after reading the data
        thisCropName = My.Name;
        InitParameters();

        // Pass on some sward variable to each species
        SetSpeciesWithSwardData();

        // Tell other modules that I exist
        AdvertiseThisCrop();

        // Send info about canopy - needed here to proper initialise micromet
        DoNewCanopyEvent();

        // write some basic initialisation info
        WriteSummary();
    }

    /// <summary>Check and initialise sward and species parameters</summary>
    private void InitParameters()
    {
        ////- Checks which species will be simulated >>>  - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
        //        . added by RCichota, Oct/2014

        // get the number of species to be simulated
        NumSpecies = speciesToSimulate.Length;

        // check that the basic parameters have been given to each species
        CheckSpeciesParameters();

        // check number of species to simulate - should be less than those we have parameters for (given here by speciesName)
        if (NumSpecies > speciesName.Length)
            throw new Exception(
                "Number of species to simulate is greater than the number of species for which parameters were given");
        if (NumSpecies < 1)
            throw new Exception("Number of species to simulate cannot be zero");

        // check names of species to simulate, look for duplicates and whether species have been parameterised
        for (int s1 = 0; s1 < NumSpecies; s1++) // s1 = species to simulate, for which parameters are being set
        {
            for (int s2 = s1 + 1; s2 < NumSpecies; s2++)
            {
                if (speciesToSimulate[s2].ToLower() == speciesToSimulate[s1].ToLower())
                    throw new Exception("The name \"" + speciesToSimulate[s1] +
                                        "\" was given more than once. Only one is allowed");
            }

            int myCount = 0;
            for (int s2 = 0; s2 < speciesName.Length; s2++) // s2 = species parameterised, where parameter come from
            {
                if (speciesToSimulate[s1].ToLower() == speciesName[s2].ToLower())
                    myCount += 1;
            }

            if (myCount < 1)
                throw new Exception("The name \"" + speciesToSimulate[s1] +
                                    "\" does not correspond to any parameterised species, check spelling");
        }

        // check whether values for parameters that may have an 'ini' setup (over-write the default ones) were given for each species
        //   assume that the parameter has negative values if not to be used
        if (iniShootDM != null)
        {
            if ((iniShootDM.Sum() > 0.0) && (iniShootDM.Length < NumSpecies))
                throw new Exception("Number of values for parameter \"iniShootDM\" is smaller than number of species");
            else
                Array.Resize(ref iniShootDM, NumSpecies);
        }
        if (iniRootDM != null)
        {
            if ((iniRootDepth.Sum() > 0.0) && (iniRootDM.Length < NumSpecies))
                throw new Exception("Number of values for parameter \"iniRootDM\" is smaller than number of species");
            else
                Array.Resize(ref iniRootDM, NumSpecies);
        }

        if (iniRootDepth != null)
        {
            if ((iniRootDepth.Sum() > 0.0) && (iniRootDepth.Length < NumSpecies))
                throw new Exception("Number of values for parameter \"iniRootDepth\" is smaller than number of species");
            else
                Array.Resize(ref iniRootDepth, NumSpecies);
        }

        if (iniRootDepthParam != null)
        {
            if ((iniRootDepthParam.Sum() > 0.0) && (iniRootDepthParam.Length < NumSpecies))
                throw new Exception(
                    "Number of values for parameter \"iniRootDepthParam\" is smaller than number of species");
            else
                Array.Resize(ref iniRootDepthParam, NumSpecies);
        }

        if (iniRootCurveParam != null)
        {
            if ((iniRootCurveParam.Sum() > 0.0) && (iniRootCurveParam.Length < NumSpecies))
                throw new Exception(
                    "Number of values for parameter \"iniRootCurveParam\" is smaller than number of species");
            else
                Array.Resize(ref iniRootCurveParam, NumSpecies);
        }

        ////- Checks arrays and soil parameters >>> - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -

        // make sure that DM fractions for initialisation have the right number of values (delete excess or add zeroes)
        //   there are 12 pools 4 for leaves, 4 for stems, and 3 for stolons
        Array.Resize(ref initialDMFractions_grass, 11);
        Array.Resize(ref initialDMFractions_legume, 11);
        Array.Resize(ref EmergenceDMFractions, 11);

        // Number of layers
        int nLayers = dlayer.Length;

        // initialise LL, same for all species (ideally this would de given for each species)
        LL_dep = new double[nLayers];
        if (ll.Length == nLayers)
        {
            // there are values for LL (so we should be using SoilWat)
            for (int layer = 0; layer < nLayers; layer++)
                LL_dep[layer] = ll[layer] * dlayer[layer];
        }
        else
        {
            // no values for LL (so we should be using SWIM - use LL15)
            for (int layer = 0; layer < nLayers; layer++)
                LL_dep[layer] = LL15_dep[layer];
        }

        // check whether values for kNO3 and kNH4 were given for all layers
        if (kNH4.Length == 1)
        {
            // if only one value was given, assume homogeneous over the profile
            Array.Resize(ref kNH4, nLayers);
            for (int layer = 1; layer < nLayers; layer++)
                kNH4[layer] = kNH4[0];
        }
        else
            Array.Resize(ref kNH4, nLayers);

        if (kNO3.Length == 1)
        {
            // if only one value was given, assume homogeneous over the profile
            Array.Resize(ref kNO3, nLayers);
            for (int layer = 1; layer < nLayers; layer++)
                kNO3[layer] = kNO3[0];
        }
        else
            Array.Resize(ref kNO3, nLayers);

        ////- Create and initialise each species >>>  - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -

        mySpecies = new Species[NumSpecies];

        // set links to static members (clock, MetData, dlayer, CO2, etc)
        Species.Clock = myClock;
        Species.MetFile = MetData;
        Species.CO2 = co2;

        maxRootDepth = new double[speciesName.Length];

        for (int s1 = 0; s1 < NumSpecies; s1++) // s1 = species to simulate, for which parameters are being set
        {
            for (int s2 = 0; s2 < speciesName.Length; s2++) // s2 = species parameterised, where parameter come from
            {
                if (speciesName[s2].ToLower() == speciesToSimulate[s1].ToLower())
                {
                    // create species and add to array
                    mySpecies[s1] = new Species();

                    // pass on values for dlayer
                    mySpecies[s1].dlayer = dlayer;
                    mySpecies[s1].xf = xf;

                    // check max root depth
                    if (iniRootDepth[s1] > 0.0)
                        maxRootDepth[s2] = iniRootDepth[s1];
                    else
                        maxRootDepth[s2] = rootDepth[s2];

                    // set the parameters and initialise the species
                    SetSpeciesParameters(s1, s2);

                    // save the initial values for the state parameters, will needed this in case of reset
                    if (iniShootDM[s1] < 0.0)
                    {
                        // if iniShootDM is negative, use the default value
                        iniShootDM[s1] = dmshoot[s2];
                    }

                    if (iniShootDM[s1] > 0.0)
                    {
                        double[] DMFractions;
                        if (mySpecies[s1].isLegume)
                            DMFractions = initialDMFractions_legume;
                        else
                            DMFractions = initialDMFractions_grass;

                        for (int pool = 0; pool < 11; pool++)
                            mySpecies[s1].InitialState.DMWeight[pool] = DMFractions[pool] * iniShootDM[s1];

                        if (iniRootDM[s1] <= 0.0)
                        {
                            // iniRootDM is zero or negative, use the default value
                            if (dmroot[s2] > 0.0)
                                iniRootDM[s1] = dmroot[s2];
                            else
                                iniRootDM[s1] = iniShootDM[s1] / mySpecies[s2].TargetSRratio;
                        }

                        if (iniRootDepth[s1] <= 0.0)
                        {
                            // iniRootDepth is zero or negative, use the default value
                            iniRootDepth[s1] = rootDepth[s2];
                        }

                        mySpecies[s1].InitialState.DMWeight[11] = iniRootDM[s1];
                        mySpecies[s1].InitialState.RootDepth = iniRootDepth[s1];
                    }
                    else
                    {
                        // shoot DM is zero, so root should also be zero
                        iniRootDepth[s1] = 0.0;
                        mySpecies[s1].InitialState.DMWeight[11] = 0.0;
                        mySpecies[s1].InitialState.RootDepth = 0.0;
                    }

                    if (iniRootDepthParam[s1] > 0.0)
                        rootTopDepthParam[s1] = iniRootDepthParam[s1];

                    if (iniRootCurveParam[s1] >= 0.0)
                        rootTopDepthParam[s1] = iniRootDepthParam[s1];

                    // assume N concentration is at optimum for green pools and minimum for dead pools
                    mySpecies[s1].InitialState.NAmount[0] = mySpecies[s1].InitialState.DMWeight[0] * mySpecies[s1].leaves.NConcOptimum;
                    mySpecies[s1].InitialState.NAmount[1] = mySpecies[s1].InitialState.DMWeight[1] * mySpecies[s1].leaves.NConcOptimum *
                                                            mySpecies[s1].NcRel2;
                    mySpecies[s1].InitialState.NAmount[2] = mySpecies[s1].InitialState.DMWeight[2] * mySpecies[s1].leaves.NConcOptimum *
                                                            mySpecies[s1].NcRel3;
                    mySpecies[s1].InitialState.NAmount[3] = mySpecies[s1].InitialState.DMWeight[3] * mySpecies[s1].leaves.NConcMinimum;
                    mySpecies[s1].InitialState.NAmount[4] = mySpecies[s1].InitialState.DMWeight[4] * mySpecies[s1].stems.NConcOptimum;
                    mySpecies[s1].InitialState.NAmount[5] = mySpecies[s1].InitialState.DMWeight[5] * mySpecies[s1].stems.NConcOptimum *
                                                            mySpecies[s1].NcRel2;
                    mySpecies[s1].InitialState.NAmount[6] = mySpecies[s1].InitialState.DMWeight[6] * mySpecies[s1].stems.NConcOptimum *
                                                            mySpecies[s1].NcRel3;
                    mySpecies[s1].InitialState.NAmount[7] = mySpecies[s1].InitialState.DMWeight[7] * mySpecies[s1].stems.NConcMinimum;
                    mySpecies[s1].InitialState.NAmount[8] = mySpecies[s1].InitialState.DMWeight[8] * mySpecies[s1].stolons.NConcOptimum;
                    mySpecies[s1].InitialState.NAmount[9] = mySpecies[s1].InitialState.DMWeight[9] * mySpecies[s1].stolons.NConcOptimum *
                                                            mySpecies[s1].NcRel2;
                    mySpecies[s1].InitialState.NAmount[10] = mySpecies[s1].InitialState.DMWeight[10] * mySpecies[s1].stolons.NConcOptimum *
                                                             mySpecies[s1].NcRel3;
                    mySpecies[s1].InitialState.NAmount[11] = mySpecies[s1].InitialState.DMWeight[11] * mySpecies[s1].roots.NConcOptimum;

                    break;
                }
            }
        }

        // check whether the sward is active (growing) or is yet to be sown
        if (iniShootDM.Sum() > 0.0)
            isAlive = true;
        else
            isAlive = false;

        // set the initial state (DM, N, LAI, etc.) for the species
        double auxLightPartition = mySpecies.Sum(species => species.lightPartitioningFactor);
        for (int s = 0; s < NumSpecies; s++)
        {
            if (!usingSpeciesRoot)
            {
                // only sward is considered, use root system of species1
                mySpecies[s].InitialState.RootDepth = iniRootDepth[0];
            }

            SetSpeciesState(s, mySpecies[s].InitialState);

            // update light partitioning factors
            mySpecies[s].lightPartitioningFactor *= NumSpecies / auxLightPartition;

            // get the deepest root as sward depth
            if (mySpecies[s].rootDepth > swardRootDepth)
            {
                swardRootDepth = mySpecies[s].rootDepth;
                swardRootZoneBottomLayer = mySpecies[s].layerBottomRootZone;
            }
        }

        // check root distribution
        swardTargetRootAllocation = new double[nLayers];
        swardRootFraction = new double[nLayers];
        if (!usingSpeciesRoot)
        {
            // only sward is considered, use root system of species1
            for (int layer = 0; layer < nLayers; layer++)
            {
                swardTargetRootAllocation[layer] = mySpecies[0].targetRootAllocation[layer];
                swardRootFraction[layer] = mySpecies[0].rootFraction[layer];
            }

            // reset root fraction of each species
            for (int s = 0; s < NumSpecies; s++)
            {
                mySpecies[s].rootFraction = new double[nLayers];
                for (int layer = 0; layer < nLayers; layer++)
                {
                    mySpecies[s].targetRootAllocation[layer] = swardTargetRootAllocation[layer];
                    mySpecies[s].rootFraction[layer] = swardRootFraction[layer];
                }
            }
        }

        // Initialising the aggregated variables (whole sward)
        UpdateAggregatedVariables();

        // Weighted average of lightExtCoeff for the sward (should be updated daily)
        double sumkLAI = mySpecies.Sum(x => x.lightExtCoeff * x.totalLAI);
        swardLightExtCoeff = MathUtility.Divide(sumkLAI, LAITotal, 1.0);

        FractionToHarvest = new double[NumSpecies];
    }

    /// <summary>Check whether all parameter have been given for each species</summary>
    private void CheckSpeciesParameters()
    {
        ////- General parameters (name and type) >>>  - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
        if (speciesName.Length < NumSpecies)
            breakCode("speciesName");
        if (micrometType.Length < NumSpecies)
            breakCode("micrometType");
        if (photoPath.Length < NumSpecies)
            breakCode("photoPath");
        if (isLegume.Length < NumSpecies)
            breakCode("isLegume");

        ////- Potential growth (photosynthesis) >>> - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
        // Photosynthesis
        if (Pm.Length < NumSpecies)
            breakCode("Pm");
        if (LightPartitioningFactor.Length < NumSpecies)
            breakCode("LightPartitioningFactor");
        if (alphaPhoto.Length < NumSpecies)
            breakCode("alphaPhoto");
        if (thetaPhoto.Length < NumSpecies)
            breakCode("thetaPhoto");
        if (fractionPAR.Length < NumSpecies)
            breakCode("fractionPAR");
        if (lightExtCoeff.Length < NumSpecies)
            breakCode("lightExtCoeff");

        // CO2 effects
        if (referenceCO2.Length < NumSpecies)
            breakCode("referenceCO2");
        if (CO2PmaxScale.Length < NumSpecies)
            breakCode("CO2PmaxScale");
        if (CO2NScale.Length < NumSpecies)
            breakCode("CO2NScale");
        if (CO2NMin.Length < NumSpecies)
            breakCode("CO2NMin");
        if (CO2NCurvature.Length < NumSpecies)
            breakCode("CO2NCurvature");

        // Temperature, general and extreme
        if (growthTmin.Length < NumSpecies)
            breakCode("growthTmin");
        if (growthTopt.Length < NumSpecies)
            breakCode("growthTopt");
        if (growthTq.Length < NumSpecies)
            breakCode("growthTq");
        if (useHeatStress.Length < NumSpecies)
            breakCode("useHeatStress");
        if (heatOnsetT.Length < NumSpecies)
            breakCode("heatOnsetT");
        if (heatFullT.Length < NumSpecies)
            breakCode("heatFullT");
        if (heatSumT.Length < NumSpecies)
            breakCode("heatSumT");
        if (heatRecoverT.Length < NumSpecies)
            breakCode("heatRecoverT");
        if (useColdStress.Length < NumSpecies)
            breakCode("useColdStress");
        if (coldOnsetT.Length < NumSpecies)
            breakCode("coldOnsetT");
        if (coldFullT.Length < NumSpecies)
            breakCode("coldFullT");
        if (coldSumT.Length < NumSpecies)
            breakCode("coldSumT");
        if (coldRecoverT.Length < NumSpecies)
            breakCode("coldRecoverT");

        // respiration
        if (growthEfficiency.Length < NumSpecies)
            breakCode("growthEfficiency");
        if (maintRespiration.Length < NumSpecies)
            breakCode("maintRespiration");
        if (respTref.Length < NumSpecies)
            breakCode("respTref");
        if (respExponent.Length < NumSpecies)
            breakCode("respExponent");

        ////- Allocation of new growth >>>  - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
        // General shoot-root
        if (MaxRootAllocation.Length < NumSpecies)
            breakCode("maxRootFraction");
        if (TargetSRratio.Length < NumSpecies)
            breakCode("targetSRratio");
        if (GlfEffectsOnSR.Length < NumSpecies)
            breakCode("GlfEffectsOnSR");

        // Effect of reproductive season
        if (UseReproSeasonFactor.Length < NumSpecies)
            breakCode("UseReproSeasonFactor");
        if (ReproSeasonReferenceLatitude.Length < NumSpecies)
            breakCode("ReproSeasonReferenceLatitude");
        if (ReproSeasonTimingCoeff.Length < NumSpecies)
            breakCode("ReproSeasonTimingCoeff");
        if (ReproSeasonDurationCoeff.Length < NumSpecies)
            breakCode("ReproSeasonDurationCoeff");
        if (ReproSeasonShouldersLengthFactor.Length < NumSpecies)
            breakCode("ReproSeasonShouldersLengthFactor");
        if (ReproSeasonOnsetDurationFactor.Length < NumSpecies)
            breakCode("ReproSeasonOnsetDurationFactor");
        if (ReproSeasonMaxAllocationIncrease.Length < NumSpecies)
            breakCode("ReproSeasonMaxAllocationIncrease");
        if (ReproSeasonAllocationCoeff.Length < NumSpecies)
            breakCode("ReproSeasonAllocationCoeff");

        // Leaf allocation
        if (FractionLeafMaximum.Length < NumSpecies)
            breakCode("maxFLeaf");
        if (FractionLeafMinimum.Length < NumSpecies)
            breakCode("minFLeaf");
        if (FractionLeafDMThreshold.Length < NumSpecies)
            breakCode("dmMaxFLeaf");
        if (FractionLeafDMFactor.Length < NumSpecies)
            breakCode("dmReferenceFLeaf");
        if (FractionLeafExponent.Length < NumSpecies)
            breakCode("exponentFLeaf");

        // Others
        if (StolonAllocationFactor.Length < NumSpecies)
            breakCode("fStolon");
        if (SpecificLeafArea.Length < NumSpecies)
            breakCode("SpecificLeafArea");
        if (SpecificRootLength.Length < NumSpecies)
            breakCode("SpecificRootLength");
        if (StolonEffectOnLAI.Length < NumSpecies)
            breakCode("StolonEffectOnLAI");
        if (ShootMaxEffectOnLAI.Length < NumSpecies)
            breakCode("ShootMaxEffectOnLAI");
        if (MaxStemEffectOnLAI.Length < NumSpecies)
            breakCode("MaxStemEffectOnLAI");

        ////- Tissue turnover and senescence >>>  - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
        if (LiveLeavesPerTiller.Length < NumSpecies)
            breakCode("liveLeavesPerTiller");
        if (rateLive2Dead.Length < NumSpecies)
            breakCode("rateLive2Dead");
        if (facGrowingTissue.Length < NumSpecies)
            breakCode("facGrowingTissue");
        if (rateDead2Litter.Length < NumSpecies)
            breakCode("rateDead2Litter");
        if (rateRootSen.Length < NumSpecies)
            breakCode("rateRootSen");
        if (massFluxTmin.Length < NumSpecies)
            breakCode("massFluxTmin");
        if (massFluxTopt.Length < NumSpecies)
            breakCode("massFluxTopt");
        if (massFluxTq.Length < NumSpecies)
            breakCode("massFluxTq");
        if (massFluxW0.Length < NumSpecies)
            breakCode("massFluxW0");
        if (massFluxWopt.Length < NumSpecies)
            breakCode("massFluxWopt");
        if (massFluxDeadWq.Length < NumSpecies)
            breakCode("massFluxDeadWq");
        if (massFluxDeadWf.Length < NumSpecies)
            breakCode("massFluxDeadWf");
        if (stockParameter.Length < NumSpecies)
            breakCode("stockParameter");
        if (Kappa2_Remob.Length < NumSpecies)
            breakCode("Kappa2_Remob");
        if (Kappa3_Remob.Length < NumSpecies)
            breakCode("Kappa3_Remob");
        if (Kappa4_Remob.Length < NumSpecies)
            breakCode("Kappa4_Remob");

        ////- N concentrations thresholds >>> - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
        if (NconcOptimum_leaves.Length < NumSpecies)
            breakCode("NconcOptimum_leaves");
        if (NconcMaximum_leaves.Length < NumSpecies)
            breakCode("NconcMaximum_leaves");
        if (NconcMinimum_leaves.Length < NumSpecies)
            breakCode("NconcMinimum_leaves");
        if (RelativeNconc_Stems.Length < NumSpecies)
            breakCode("RelativeNconc_Stems");
        if (RelativeNconc_Stolons.Length < NumSpecies)
            breakCode("RelativeNconc_Stolons");
        if (RelativeNconc_Roots.Length < NumSpecies)
            breakCode("RelativeNconc_Roots");
        if (RelativeNconc_stage2.Length < NumSpecies)
            breakCode("RelativeNconc_stage2");
        if (RelativeNconc_stage3.Length < NumSpecies)
            breakCode("RelativeNconc_stage3");

        ////- N fixation >>>  - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
        if (NMinFix.Length < NumSpecies)
            breakCode("NMinFix");
        if (NMaxFix.Length < NumSpecies)
            breakCode("NMaxFix");
        if (symbiontCostFactor.Length < NumSpecies)
            breakCode("symbiontCostFactor");
        if (NFixingCostFactor.Length < NumSpecies)
            breakCode("NFixingCostFactor");

        ////- Growth limiting factors >>> - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
        if (soilSatFactor.Length < NumSpecies)
            breakCode("soilSatFactor");
        if (MinMacroPorosity.Length < NumSpecies)
            breakCode("MinMacroPorosity");
        if (SoilSaturationRecoveryFactor.Length < NumSpecies)
            breakCode("SoilSaturationRecoveryFactor");
        if (NdilutCoeff.Length < NumSpecies)
            breakCode("NdilutCoeff");
        if (GenericGLF.Length < NumSpecies)
            breakCode("GenericGLF");
        if (SFertilityGLF.Length < NumSpecies)
            breakCode("SFertilityGLF");

        ////- Plant height >>>  - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
        if (MaxPlantHeight.Length < NumSpecies)
            breakCode("MaxPlantHeight");
        if (MinimumHeight.Length < NumSpecies)
            breakCode("MinimumHeight");
        if (MassForMaxHeight.Length < NumSpecies)
            breakCode("MassForMaxHeight");
        if (ExponentHeightFromMass.Length < NumSpecies)
            breakCode("ExponentHeightFromMass");

        ////- Root depth and distribution >>> - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
        if (rootDepth.Length < NumSpecies)
            breakCode("rootDepth");
        if (ExpoLinearDepthParam.Length < NumSpecies)
            breakCode("ExpoLinearDepthParam");
        if (ExpoLinearCurveParam.Length < NumSpecies)
            breakCode("ExpoLinearCurveParam");
        if (refRLD.Length < NumSpecies)
            breakCode("referenceRLD");
        if (ExponentSWCUptake.Length < NumSpecies)
            breakCode("ExponentSWCUptake");
        if (MaximumUptakeRateNH4.Length < NumSpecies)
            breakCode("MaximumUptakeRateNH4");
        if (MaximumUptakeRateNO3.Length < NumSpecies)
            breakCode("MaximumUptakeRateNO3");

        ////- Digestibility and feed quality >>>  - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
        if (digestLiveCellWall.Length < NumSpecies)
            breakCode("digestLive");
        if (digestDeadCellWall.Length < NumSpecies)
            breakCode("digestDead");
        if (SugarFractionNewGrowth.Length < NumSpecies)
            breakCode("SugarFractionNewGrowth");

        ////- Harvest limits and preferences >>>  - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
        if (dmshoot.Length < NumSpecies)
            breakCode("dmshoot");
        if (dmroot.Length < NumSpecies)
            breakCode("dmroot");
        if (dmgreenmin.Length < NumSpecies)
            breakCode("dmgreenmin");
        if (FractionStolonsStanding.Length < NumSpecies)
            breakCode("FractionStolonsStanding");
        if (PreferenceForGreenDM.Length < NumSpecies)
            breakCode("PreferenceForGreenDM");
        if (PreferenceForLeaves.Length < NumSpecies)
            breakCode("PreferenceForLeaves");
    }

    /// <summary>Throw an exception error about wrong parameter set up, with message</summary>
    /// <param name="myVariable"></param>
    private void breakCode(string myVariable)
    {
        throw new Exception("Number of values for paramater \"" + myVariable + "\" is smaller than number of species");
    }

    /// <summary>Set parameter values for each species in the sward</summary>
    /// <param name="s1">The index for the species being set up</param>
    /// <param name="s2">The index for the species in the parameter set</param>
    private void SetSpeciesParameters(int s1, int s2)
    {
        ////- General parameters (name and type) >>>  - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
        mySpecies[s1].speciesName = speciesName[s2];
        mySpecies[s1].micrometType = micrometType[s2];

        mySpecies[s1].isLegume = (int) isLegume[s2] == 1;
        mySpecies[s1].photoPath = "C" + (int) photoPath[s2];

        ////- Parameters for germination and annual species >>> - - - - - - - - - - - - - - - - - - - - - - - - - - - -
        // NOTE: deactivating all the annual stuff (never really fully implemented)
        // if (isAnnual[s] == 1) SP[s].isAnnual = true;
        // else SP[s].isAnnual = false;
        mySpecies[s1].isAnnual = false;

        mySpecies[s1].dayGermn = 220; // (int)dayEmerg[s];
        mySpecies[s1].daysEmgToAnth = 100; //(int)monEmerg[s];
        mySpecies[s1].daysAnthToMatur = 100; //(int)dayAnth[s];

        // Parameters for germination and emergence
        mySpecies[s1].degreesdayForGermination = DegreesDayForGermination[s2];
        int nTissues = EmergenceDMFractions.Length;
        mySpecies[s1].emergenceDM = new double[nTissues + 1];
        for (int p = 0; p < nTissues; p++)
            mySpecies[s1].emergenceDM[p] = EmergenceDMFractions[p] * dmgreenmin[s1];
        mySpecies[s1].emergenceDM[nTissues] = dmgreenmin[s1]; // rootDM at germination equals shootDM

        ////- Potential growth (photosynthesis) >>> - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
        // Photosynthesis
        mySpecies[s1].Pm = Pm[s2];
        mySpecies[s1].lightPartitioningFactor = LightPartitioningFactor[s2];
        mySpecies[s1].alphaPhoto = alphaPhoto[s2];
        mySpecies[s1].thetaPhoto = thetaPhoto[s2];
        mySpecies[s1].fractionPAR = fractionPAR[s2];
        mySpecies[s1].lightExtCoeff = lightExtCoeff[s2];

        // CO2 effects
        mySpecies[s1].referenceCO2 = referenceCO2[s2];
        mySpecies[s1].CO2PmaxScale = CO2PmaxScale[s2];
        mySpecies[s1].CO2NScale = CO2NScale[s2];
        mySpecies[s1].CO2NMin = CO2NMin[s2];
        mySpecies[s1].CO2NCurvature = CO2NCurvature[s2];

        // Temperature, general and extreme
        mySpecies[s1].growthTmin = growthTmin[s2];
        mySpecies[s1].growthTopt = growthTopt[s2];
        mySpecies[s1].growthTq = growthTq[s2];
        mySpecies[s1].usingHeatStress = useHeatStress[s2].ToLower() == "yes";
        mySpecies[s1].heatOnsetT = heatOnsetT[s2];
        mySpecies[s1].heatFullT = heatFullT[s2];
        mySpecies[s1].heatSumT = heatSumT[s2];
        mySpecies[s1].heatRecoverT = heatRecoverT[s2];
        mySpecies[s1].usingColdStress = useColdStress[s2].ToLower() == "yes";
        mySpecies[s1].coldOnsetT = coldOnsetT[s2];
        mySpecies[s1].coldFullT = coldFullT[s2];
        mySpecies[s1].coldSumT = coldSumT[s2];
        mySpecies[s1].coldRecoverT = coldRecoverT[s2];

        // Respiration
        mySpecies[s1].maintRespiration = maintRespiration[s2] * 0.01; // converted from %
        mySpecies[s1].growthEfficiency = growthEfficiency[s2];
        mySpecies[s1].respExponent = respExponent[s2];
        mySpecies[s1].respTref = respTref[s2];

        ////- Allocation of new growth >>>  - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
        mySpecies[s1].MaxRootAllocation = MaxRootAllocation[s2];
        mySpecies[s1].TargetSRratio = TargetSRratio[s2];
        mySpecies[s1].GlfEffectOnSR = GlfEffectsOnSR[s2];
        mySpecies[s1].UsingReproSeasonFactor = UseReproSeasonFactor[s2].ToLower() == "yes";
        mySpecies[s1].ReproSeasonReferenceLatitude = ReproSeasonReferenceLatitude[s2];
        mySpecies[s1].ReproSeasonTimingCoeff = ReproSeasonTimingCoeff[s2];
        mySpecies[s1].ReproSeasonDurationCoeff = ReproSeasonDurationCoeff[s2];
        mySpecies[s1].ReproSeasonShouldersLengthFactor = ReproSeasonShouldersLengthFactor[s2];
        mySpecies[s1].ReproSeasonOnsetDurationFactor = ReproSeasonOnsetDurationFactor[s2];
        mySpecies[s1].ReproSeasonMaxAllocationIncrease = ReproSeasonMaxAllocationIncrease[s2];
        mySpecies[s1].ReproSeasonAllocationCoeff = ReproSeasonAllocationCoeff[s2];
        mySpecies[s1].InitReproductiveGrowthFactor();
        mySpecies[s1].FractionLeafMaximum = FractionLeafMaximum[s2];
        mySpecies[s1].FractionLeafMinimum = FractionLeafMinimum[s2];
        mySpecies[s1].FractionLeafDMThreshold = FractionLeafDMThreshold[s2];
        mySpecies[s1].FractionLeafDMFactor = FractionLeafDMFactor[s2];
        mySpecies[s1].FractionLeafExponent = FractionLeafExponent[s2];
        mySpecies[s1].StolonAllocationFactor = StolonAllocationFactor[s2];

        mySpecies[s1].SpecificLeafArea = SpecificLeafArea[s2];
        mySpecies[s1].SpecificRootLength = SpecificRootLength[s2];
        mySpecies[s1].StolonEffectOnLAI = StolonEffectOnLAI[s2];
        mySpecies[s1].ShootMaxEffectOnLAI = ShootMaxEffectOnLAI[s2];
        mySpecies[s1].MaxStemEffectOnLAI = MaxStemEffectOnLAI[s2];

        ////- Tissue turnover and senescence >>>  - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
        mySpecies[s1].LiveLeavesPerTiller = LiveLeavesPerTiller[s2];
        mySpecies[s1].refTissueTurnoverRate = rateLive2Dead[s2];
        mySpecies[s1].facGrowingTissue = facGrowingTissue[s2];
        mySpecies[s1].refLitteringRate = rateDead2Litter[s2];
        mySpecies[s1].rateRootSen = rateRootSen[s2];
        mySpecies[s1].massFluxTmin = massFluxTmin[s2];
        mySpecies[s1].massFluxTopt = massFluxTopt[s2];
        mySpecies[s1].massFluxTq = massFluxTq[s2];
        mySpecies[s1].massFluxW0 = massFluxW0[s2];
        mySpecies[s1].massFluxWopt = massFluxWopt[s2];
        mySpecies[s1].exponentGLFW2dead = massFluxDeadWq[s2];
        mySpecies[s1].factorGLFW2dead = massFluxDeadWf[s2];
        mySpecies[s1].stockParameter = stockParameter[s2];
        mySpecies[s1].Kappa2 = Kappa2_Remob[s2];
        mySpecies[s1].Kappa3 = Kappa3_Remob[s2];
        mySpecies[s1].Kappa4 = Kappa4_Remob[s2];

        ////- N concentrations thresholds >>> - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
        mySpecies[s1].NcRel2 = RelativeNconc_stage2[s2];
        mySpecies[s1].NcRel3 = RelativeNconc_stage3[s2];

        // Note: 0.01 is for conversion of % to fraction
        mySpecies[s1].leaves.NConcOptimum = 0.01 * NconcOptimum_leaves[s2];
        mySpecies[s1].stems.NConcOptimum = mySpecies[s1].leaves.NConcOptimum * RelativeNconc_Stems[s2];
        mySpecies[s1].stolons.NConcOptimum = mySpecies[s1].leaves.NConcOptimum * RelativeNconc_Stolons[s2];
        mySpecies[s1].roots.NConcOptimum = mySpecies[s1].leaves.NConcOptimum * RelativeNconc_Roots[s2];

        mySpecies[s1].leaves.NConcMaximum = 0.01 * NconcMaximum_leaves[s2];
        mySpecies[s1].stems.NConcMaximum = mySpecies[s1].leaves.NConcMaximum * RelativeNconc_Stems[s2];
        mySpecies[s1].stolons.NConcMaximum = mySpecies[s1].leaves.NConcMaximum * RelativeNconc_Stolons[s2];
        mySpecies[s1].roots.NConcMaximum = mySpecies[s1].leaves.NConcMaximum * RelativeNconc_Roots[s2];

        mySpecies[s1].leaves.NConcMinimum = 0.01 * NconcMinimum_leaves[s2];
        mySpecies[s1].stems.NConcMinimum = mySpecies[s1].leaves.NConcMinimum * RelativeNconc_Stems[s2];
        mySpecies[s1].stolons.NConcMinimum = mySpecies[s1].leaves.NConcMinimum * RelativeNconc_Stolons[s2];
        mySpecies[s1].roots.NConcMinimum = mySpecies[s1].leaves.NConcMinimum * RelativeNconc_Roots[s2];

        ////- N fixation >>>  - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
        mySpecies[s1].MaxFix = NMaxFix[s2];
        mySpecies[s1].MinFix = NMinFix[s2];
        mySpecies[s1].NFixationCostMethod = NFixationCostMethod;
        mySpecies[s1].symbiontCostFactor = symbiontCostFactor[s2];
        mySpecies[s1].NFixingCostFactor = NFixingCostFactor[s2];

        ////- Growth limiting factors >>> - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
        mySpecies[s1].NdilutCoeff = NdilutCoeff[s2];
        mySpecies[s1].soilSatFactor = soilSatFactor[s2];
        mySpecies[s1].minMacroPorosity = MinMacroPorosity[s2];
        mySpecies[s1].saturationRecoveryFactor = SoilSaturationRecoveryFactor[s2];
        mySpecies[s1].GLFSFertility = SFertilityGLF[s2];
        mySpecies[s1].GLFGeneric = GenericGLF[s2];

        ////- Plant height >>>  - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
        mySpecies[s1].MaxPlantHeight = MaxPlantHeight[s2];
        mySpecies[s1].MassForMaxHeight = MassForMaxHeight[s2];
        mySpecies[s1].ExponentHeightFromMass = ExponentHeightFromMass[s2];
        mySpecies[s1].MinimumHeight = MinimumHeight[s2];

        ////- Root depth and distribution >>> - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
        mySpecies[s1].minRootDepth = MinimumRootDepth[s2];
        mySpecies[s1].maxRootDepth = maxRootDepth[s2];
        mySpecies[s1].rootElongationRate = RootElongationRate[s2];
        if (usingSpeciesRoot)
        {
            // root specified for each species
            mySpecies[s1].expoLinearDepthParam = rootTopDepthParam[s2];
            mySpecies[s1].expoLinearCurveParam = rootCurveParam[s2];
            mySpecies[s1].MaximumUptakeRateNH4 = MaximumUptakeRateNH4[s2];
            mySpecies[s1].MaximumUptakeRateNO3 = MaximumUptakeRateNO3[s2];
            mySpecies[s1].referenceRLD = refRLD[s2];
            mySpecies[s1].exponentSWCuptake = ExponentSWCUptake[s2];
        }
        else
        {
            // root specified for whole sward (use first species as data entry)
            mySpecies[s1].expoLinearDepthParam = rootTopDepthParam[0];
            mySpecies[s1].expoLinearCurveParam = rootCurveParam[0];
            mySpecies[s1].MaximumUptakeRateNH4 = MaximumUptakeRateNH4[0];
            mySpecies[s1].MaximumUptakeRateNO3 = MaximumUptakeRateNO3[0];
            mySpecies[s1].referenceRLD = refRLD[0];
            mySpecies[s1].exponentSWCuptake = ExponentSWCUptake[0];
        }

        ////- Digestibility and feed quality >>>  - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
        mySpecies[s1].leaves.DigestLiveCellWall = digestLiveCellWall[s2];
        mySpecies[s1].leaves.DigestDeadCellWall = digestDeadCellWall[s2];
        mySpecies[s1].stems.DigestLiveCellWall = digestLiveCellWall[s2];
        mySpecies[s1].stems.DigestDeadCellWall = digestDeadCellWall[s2];
        mySpecies[s1].stolons.DigestLiveCellWall = digestLiveCellWall[s2];
        mySpecies[s1].stolons.DigestDeadCellWall = digestDeadCellWall[s2];
        mySpecies[s1].fToSugar = SugarFractionNewGrowth[s2];

        ////- Harvest limits and preferences >>>  - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
        mySpecies[s1].leaves.MinimumGreenDM = dmgreenmin[s2] * 0.80;
        mySpecies[s1].stems.MinimumGreenDM = dmgreenmin[s2] * 0.20;
        mySpecies[s1].stolons.MinimumGreenDM = 0.0;
        mySpecies[s1].roots.MinimumGreenDM = dmgreenmin[s2] * 0.50;
        mySpecies[s1].stolons.FractionStanding = FractionStolonsStanding[s2];
        mySpecies[s1].PreferenceGreenOverDead = PreferenceForGreenDM[s2];
        mySpecies[s2].PreferenceLeafOverStem = PreferenceForLeaves[s2];
        if ((mySpecies[s1].PreferenceGreenOverDead < Epsilon) || (mySpecies[s1].PreferenceGreenOverDead < Epsilon))
            throw new Exception("Relative preferences for green or leaf DM cannot be set to zero");

        ////- Additional initialisation bits >>>  - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
        mySpecies[s1].ShootAllocationFactor = 1.0;

        int nLayers = dlayer.Length;
        soilAvailableWater = new double[nLayers];
        soilWaterUptake = new double[nLayers];
        soilNH4Available = new double[nLayers];
        soilNH4Uptake = new double[nLayers];
        soilNO3Available = new double[nLayers];
        soilNO3Uptake = new double[nLayers];

        mySpecies[s1].soilAvailableWater = new double[nLayers];
        mySpecies[s1].soilWaterUptake = new double[nLayers];
        mySpecies[s1].soilAvailableNH4 = new double[nLayers];
        mySpecies[s1].soilAvailableNO3 = new double[nLayers];
    }

    /// <summary>Set DM and N values for each species in the sward</summary>
    /// <param name="s">The index for the species being set up</param>
    /// <param name="MyState">The collection of basic state defining values</param>
    private void SetSpeciesState(int s, SpeciesBasicState MyState)
    {
        // General state (DM and N)
        mySpecies[s].isAlive = isAlive;
        if (MyState.DMWeight.Sum() <= 1e-12)
            mySpecies[s].phenoStage = 0;
        else
            mySpecies[s].phenoStage = 1;

        mySpecies[s].SetSpeciesState(MyState);

        // Root depth and distribution
        mySpecies[s].layerBottomRootZone = mySpecies[s].GetRootZoneBottomLayer();
    }

    /// <summary>Let other module (micromet and SWIM) know about the existence of this crop (sward)</summary>
    /// <remarks>
    ///  Ideally we should advertise each species, another module would do the resource arbitration.
    ///  However, if we were to do this we would have to supply some data (outputs) for the modules
    ///  that respond to the presence of a crop (SWIM and micromet):
    ///  the events 'New_Canopy' and 'NewPotentialGrowth' are used by micromet to get the data, while
    ///  SWIM requires some output variables (RLV and WaterDemand). Raising events for each species works
    ///  fine, but to have outputs for each species would require changing a lot of code and how a species
    ///  is interpreted by APSIM (It would need to be a module on its own right. SWIM needs to registers
    ///  each crop module, whose outputs it will ask for when it needs). This seem possible to do, but
    ///  requires time. It will be left as it is for now. The resource arbitration has to be done from
    ///  within AgPasture (RCichota, Nov2014)
    /// </remarks>
    private void AdvertiseThisCrop()
    {
        NewCropType cropData = new NewCropType();

        cropData.crop_type = micrometType[0];
        cropData.sender = thisCropName;
        NewCrop.Invoke(cropData);
    }

    /// <summary>Write initialisation info to summary file</summary>
    private void WriteSummary()
    {
        Console.WriteLine();
        Console.Write(@"
           AgPature Properties
         -----------------------------------------------------------------------------
          Species        TotalWt  ShootWt  RootWt   LAI  TotalC   TotalN   RootDepth
                         (kg/ha)  (kg/ha)  (kg/ha)   () (kg/ha)   (kg/ha)       (mm)
         -----------------------------------------------------------------------------
");
        for (int specie = 0; specie < mySpecies.Length; ++specie)
        {
            Console.WriteLine(
                "          {0,-12}    {1,6:F1}   {2,6:F1}  {3,6:F1}  {4,4:F2}  {5,6:F1}    {6,5:F1}      {7,6:F1}",
                mySpecies[specie].speciesName,
                mySpecies[specie].AboveGroundWt + mySpecies[specie].roots.DMTotal,
                mySpecies[specie].AboveGroundWt,
                mySpecies[specie].roots.DMTotal,
                mySpecies[specie].totalLAI,
                (mySpecies[specie].AboveGroundWt + mySpecies[specie].roots.DMTotal) * 0.4,
                mySpecies[specie].AboveGroundN + mySpecies[specie].roots.NTotal,
                mySpecies[specie].rootDepth);
        }

        Console.WriteLine("         -----------------------------------------------------------------------------");
        Console.WriteLine(
            "          Totals          {0,6:F1}   {1,6:F1}  {2,6:F1}  {3,4:F2}  {4,6:F1}    {5,5:F1}      {6,6:F1}",
            TotalWt, AboveGroundWt, BelowGroundWt, LAITotal, TotalC, TotalN, swardRootDepth);
        Console.WriteLine("         -----------------------------------------------------------------------------");

        Console.WriteLine();
        Console.WriteLine("          - N uptake controlled by " +
                          ((NUptakeSource == "calc") ? "AgPasture" : "an external module"));
        Console.WriteLine("          - Water uptake controlled by " +
                          ((WaterUptakeSource == "calc") ? "AgPasture" : "an external module"));
        Console.WriteLine();

        Console.Write(@"
          Root distribution
         -----------------------------
          Layer     Depth  FractionWt
         -----------------------------
");
        double LayerTop = 0;
        for (int layer = 0; layer < dlayer.Length; layer++)
        {
            Console.WriteLine("          {0,3}  {1,10}     {2,6:F3}", layer,
                LayerTop.ToString() + "-" + (LayerTop + dlayer[layer]).ToString(), swardRootFraction[layer]);
            LayerTop += dlayer[layer];
        }

        Console.WriteLine("         -----------------------------");
    }

    #endregion  ------------------------------------------------------------------------------------------------------------

    #region Daily processes  -----------------------------------------------------------------------------------------------

    /// <summary>EventHandler - get new met data (not really used)</summary>
    /// <param name="NewMetData">Weather data</param>
    [EventHandler]
    public void OnNewMet(NewMetType NewMetData)
    {
        for (int s = 0; s < NumSpecies; s++)
            mySpecies[s].RefreshVariables();
    }

    /// <summary>EventHandler - preparation before the main process</summary>
    [EventHandler]
    public void OnPrepare()
    {
        // Clear FractionHarvest array
        Array.Clear(FractionToHarvest, 0, FractionToHarvest.Length);

        // Send info about canopy and potential growth, used by other modules to calculate intercepted radn and ET
        DoNewCanopyEvent();
        DoNewPotentialGrowthEvent();
    }

    /// <summary>Perform the main process phase</summary>
    [EventHandler]
    public void OnProcess()
    {
        if (isAlive)
        {
            // Pass on some parameters to different species
            SetSpeciesWithSwardData();

            // Allocate resources for each species (includes water demand)
            PartitionAboveGroundResources();

            // Get potential growth
            swardNetPotentialGrowth = 0;
            for (int s = 0; s < NumSpecies; s++)
            {
                if (mySpecies[s].phenoStage == 0)
                {
                    // not germinated yet, check germination progress
                    if (mySpecies[s].DailyGerminationProgress() >= 1.0)
                    {
                        // germination completed
                        mySpecies[s].SetEmergenceState();
                    }
                }
                else
                {
                    // Compute the tissue turnover
                    mySpecies[s].EvaluateTissueTurnover();

                    if (usingSpeciesPhotosynthesis)
                        swardNetPotentialGrowth += mySpecies[s].CalcDailyPotentialGrowth();
                    else
                        swardNetPotentialGrowth += mySpecies[s].DailyGrowthPot();
                }

                // Evaluate potential allocation of today's growth
                mySpecies[s].EvaluateAllocationFractions();
            }

            // Evaluate the water supply, demand & uptake
            DoWaterCalculations();

            // Get the potential growth after water limitations
            SetSpeciesGLFWater();
            SetSpeciesGLFAeration();
            swardPotGrowthAfterWater = 0.0;
            for (int s = 0; s < NumSpecies; s++)
                swardPotGrowthAfterWater += mySpecies[s].CalcGrowthAfterWaterLimitations();

            // Get the N amount demanded for optimum growth and luxury uptake
            EvaluateNitrogenDemand();

            // Evaluate the nitrogen soil demand, supply, and uptake
            DoNitrogenCalculations();

            // Get the actual growth, after nutrient limitations but before senescence
            SetSpeciesGLFNitrogen();
            swardPotGrowthAfterNutrient = 0.0;
            for (int s = 0; s < NumSpecies; s++)
                swardPotGrowthAfterNutrient += mySpecies[s].CalcGrowthAfterNLimitations();

            for (int s = 0; s < NumSpecies; s++)
            {
                // Evaluate actual allocation of today's growth
                mySpecies[s].EvaluateAllocationNewGrowth();

                // Check changes in root depth and update root distribution
                if (usingSpeciesRoot)
                    mySpecies[s].EvaluateRootGrowth();
                else
                    EvaluateRootGrowth(s);

                // Update the DM and N of each tissue
                mySpecies[s].DoUpdateTissues();

                // Update the aggregated variables (LAI, height, etc)
                mySpecies[s].UpdateAggregatedVariables();
            }

            // Update aggregated variables (whole sward)
            UpdateAggregatedVariables();

            // Send detached material to other modules (litter to surfaceOM, roots to soilFOM) 
            DoSurfaceOMReturn(swardLitterDM, swardLitterN, 1.0);
            DoIncorpFomEvent(swardSenescedRootDM, swardSenescedRootN);

            // Do the actual uptake (send changes to other modules)
            DoSoilWaterUptake();
            DoSoilNUptake();
        }
    }

    #region - Resource balance processes  - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -

    /// <summary>Send out info about canopy</summary>
    /// <remarks>
    ///  - micromet uses to compute radiation interception and ET
    ///  Ideally we should pass the values for each species, micromet would then do the resource arbitration.
    ///  However, this is not possible due to conflict with SWIM (see comment on AdvertiseThisCrop())
    ///  </remarks>
    private void DoNewCanopyEvent()
    {
        NewCanopyType canopyData = new NewCanopyType();

        //  Pack and send info about the average sward canopy
        canopyData.sender = thisCropName;
        canopyData.lai = (float) LAIGreen;
        canopyData.lai_tot = (float) LAITotal;
        canopyData.height = (int) Height;
        canopyData.depth = (int) Height;
        canopyData.cover = (float) CoverGreen;
        canopyData.cover_tot = (float) CoverTotal;

        New_Canopy.Invoke(canopyData);
    }

    /// <summary>Send out info about potential limitation to growth</summary>
    /// <remarks>
    ///  - micromet uses this to compute radiation interception and ET
    ///  Ideally we should pass the values for each species, micromet would then do the resource arbitration.
    ///  However, this is not possible due to conflicting comms with SWIM (see comment on AdvertiseThisCrop())
    /// </remarks>
    private void DoNewPotentialGrowthEvent()
    {
        double Tday = (0.75 * MetData.MaxT) + (0.25 * MetData.MinT);
        swardGLFTemp = 0.0; // this will be the glfTemp output, as weighted average
        for (int s = 0; s < NumSpecies; s++)
        {
            double prop = 1.0 / NumSpecies;
            if (AboveGroundLiveWt != 0.0)
            {
                prop = MathUtility.Divide(mySpecies[s].AboveGroundLiveWt, AboveGroundLiveWt, 1.0);
            }

            mySpecies[s].glfTemp = mySpecies[s].GFTemperature(Tday);
            swardGLFTemp += mySpecies[s].glfTemp * prop;
        }

        double gft = 1.0;
        if (Tday < 20.0)
        {
            gft = Math.Sqrt(swardGLFTemp);
        }
        else
        {
            gft = swardGLFTemp;
        }
        // Note: swardGLFTemp is for gross photosythesis.
        // This is different from that for net production as used in other APSIM crop models, and is
        // assumed in calculation of temperature effect on transpiration (in micromet).
        // Here we passed it as sqrt - (Doing so by a comparison of swardGLFTemp and that
        // used in wheat). Temperature effects on NET production of forage species in other models
        // (e.g., grassgro) are not so significant for T = 10-20 degrees(C) - [Frank Li]

        frgr = Math.Min(FVPD, gft);
        frgr = Math.Min(frgr, GlfGeneric);

        // Pack and send the information
        NewPotentialGrowthType PGrowthData = new NewPotentialGrowthType();
        PGrowthData.sender = thisCropName;
        PGrowthData.frgr = (float) frgr;

        NewPotentialGrowth.Invoke(PGrowthData);
    }

    /// <summary>Get plant potential transpiration (from micromet)</summary>
    /// <param name="waterDemandData">plant water demand</param>
    [EventHandler]
    public void OnCanopy_Water_Balance(CanopyWaterBalanceType waterDemandData)
    {
        swardWaterDemand = 0.0;
        for (int i = 0; i < waterDemandData.Canopy.Length; i++)
        {
            if (waterDemandData.Canopy[i].name.ToUpper() == thisCropName.ToUpper())
            {
                swardWaterDemand = waterDemandData.Canopy[i].PotentialEp;
                // Note: water demand for whole sward, partition done later
            }
        }
    }

    /// <summary>Get light interception data (energy balance)</summary>
    /// <param name="lightInterceptionData">light interception data</param>
    [EventHandler]
    public void OnCanopy_Energy_Balance(CanopyEnergyBalanceType lightInterceptionData)
    {
        InterceptedRadn = 0.0;
        for (int i = 0; i < lightInterceptionData.Interception.Length; i++)
        {
            if (lightInterceptionData.Interception[i].name.ToUpper() == thisCropName.ToUpper())
            {
                for (int j = 0; j < lightInterceptionData.Interception[i].layer.Length; j++)
                {
                    InterceptedRadn += lightInterceptionData.Interception[i].layer[j].amount;
                    // light interception considered for whole sward, partition between species done later
                }
            }
        }
    }

    /// <summary>Estimate the allocation of intercepted radiation and ET for each species</summary>
    /// <remarks>
    /// Intercepted solar Radn and ET were considered (by micromet) for whole sward, so need to partition here
    /// Partition between species is based on LAI and lightExtCoeff, following micromet's approach
    /// note: original AgPasture used green cover
    /// </remarks>
    private void PartitionAboveGroundResources()
    {
        double sumkLAI = 0.0;
        for (int s = 0; s < NumSpecies; s++)
        {
            sumkLAI += mySpecies[s].greenLAI * mySpecies[s].lightExtCoeff;
        }

        for (int s = 0; s < NumSpecies; s++)
        {
            if (sumkLAI == 0.0)
            {
                mySpecies[s].intRadnFrac = 0.0;
                mySpecies[s].interceptedRadn = 0.0;
                mySpecies[s].WaterDemand = 0.0;
            }
            else
            {
                mySpecies[s].intRadnFrac = mySpecies[s].greenLAI * mySpecies[s].lightExtCoeff / sumkLAI;
                mySpecies[s].interceptedRadn = InterceptedRadn * mySpecies[s].intRadnFrac;
                mySpecies[s].WaterDemand = swardWaterDemand * mySpecies[s].intRadnFrac;
            }
        }
    }

    /// <summary>Let species know the value of some sward variables</summary>
    private void SetSpeciesWithSwardData()
    {
        // pass radiation and canopy properties to species
        Species.swardInterceptedRadn = InterceptedRadn;
        Species.swardCoverGreen = CoverGreen;
        Species.swardLightExtCoeff = swardLightExtCoeff;

        // pass soil values to species
        for (int s = 0; s < NumSpecies; s++)
        {
            mySpecies[s].dlayer = dlayer;
            mySpecies[s].xf = xf;
        }
    }

    #endregion  - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -

    #region - Plant growth processes  - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -

    /// <summary>
    /// Calculates variations in root growth and distribution, when using sward only
    /// </summary>
    private void EvaluateRootGrowth(int sp)
    {
        if (sp == 0)
        {
            mySpecies[sp].EvaluateRootGrowth();
            swardRootDepth = mySpecies[sp].rootDepth;
            swardRootZoneBottomLayer = mySpecies[sp].layerBottomRootZone;
            for (int layer = 0; layer < dlayer.Length; layer++)
                swardRootFraction[layer] = mySpecies[sp].rootFraction[layer];
        }
        else
        {
            mySpecies[sp].rootDepth = swardRootDepth;
            mySpecies[sp].layerBottomRootZone = swardRootZoneBottomLayer;
            for (int layer = 0; layer < dlayer.Length; layer++)
                mySpecies[sp].rootFraction[layer] = swardRootFraction[layer];
        }
    }

    /// <summary>Update the values of variables for whole plant parts and the sward</summary>
    private void UpdateAggregatedVariables()
    {
        // reset some variables
        swardLitterDM = 0.0;
        swardLitterN = 0.0;
        swardSenescedRootDM = 0.0;
        swardSenescedRootN = 0.0;
        double sumkLAI = 0.0;

        for (int s = 0; s < NumSpecies; s++)
        {
            //accumulate the DM and N for all species
            swardLitterDM += mySpecies[s].dDMLitter;
            swardLitterN += mySpecies[s].dNLitter;
            swardSenescedRootDM += mySpecies[s].dDMRootSen;
            swardSenescedRootN += mySpecies[s].dNRootSen;

            //accumulate this for weighted average of lightExtCoeff
            sumkLAI += mySpecies[s].lightExtCoeff * mySpecies[s].greenLAI;
        }

        // get sward light extinction coefficient
        if (updateLightExtCoeffAllowed)
        {
            swardLightExtCoeff = MathUtility.Divide(sumkLAI, LAIGreen, 1.0);
        }

        // get sward average root depth and distribution
        if (usingSpeciesRoot)
        {
            // the deepest root depth is used for sward
            swardRootDepth = mySpecies.Max(x => x.rootDepth);
            swardRootZoneBottomLayer = mySpecies.Max(x => x.layerBottomRootZone);
            for (int layer = 0; layer < dlayer.Length; layer++)
            {
                for (int s = 0; s < NumSpecies; s++)
                {
                    swardRootFraction[layer] += mySpecies[s].roots.DMGreen * mySpecies[s].rootFraction[layer];
                    swardTargetRootAllocation[layer] += mySpecies[s].roots.DMGreen * mySpecies[s].targetRootAllocation[layer];
                }

                if (RootWt > 0.0)
                {
                    swardRootFraction[layer] /= RootWt;
                    swardTargetRootAllocation[layer] /= RootWt;
                }
                else
                {
                    swardRootFraction[layer] = 0.0;
                    swardTargetRootAllocation[layer] = 0.0;
                }
            }
        }
        //else  root distribution already updated (in GrowthAndPartition) 
    }

    #endregion  - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -

    #region - Water uptake processes  - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -

    /// <summary>
    /// Evaluate water supply vs. demand and compute water uptake
    /// </summary>
    private void DoWaterCalculations()
    {
        // Get soil water available in the root zone
        soilAvailableWater = PlantWaterAvailability();

        double wFrac = 0.0; // uptake fraction for each species
        int nLayers = dlayer.Length;

        // clear some variables
        Array.Clear(soilWaterUptake, 0, nLayers);

        double totalWaterUptake = 0.0;
        if (WaterUptakeSource.ToLower() == "calc")
        {
            // uptake is calculated by AgPasture
            if (usingWUptakeBySpecies)
            {
                // consider each species
                double totAvailable;
                double totUptake;
                for (int s = 0; s < NumSpecies; s++)
                {
                    totAvailable = mySpecies[s].soilAvailableWater.Sum();
                    totUptake = Math.Min(mySpecies[s].WaterDemand, totAvailable);
                    if (totUptake > 0.0)
                    {
                        wFrac = Math.Min(1.0, Math.Max(0.0, totUptake / totAvailable));
                        for (int layer = 0; layer < nLayers; layer++)
                        {
                            mySpecies[s].soilWaterUptake[layer] = mySpecies[s].soilAvailableWater[layer] * wFrac;
                            soilWaterUptake[layer] += mySpecies[s].soilWaterUptake[layer];
                        }
                    }
                    else
                    {
                        Array.Clear(mySpecies[s].soilWaterUptake, 0, nLayers);
                    }
                }

                totalWaterUptake += soilWaterUptake.Sum();
            }
            else
            {
                // consider only whole sward
                double sFrac;
                double totAvailable = soilAvailableWater.Sum();
                totalWaterUptake = Math.Min(swardWaterDemand, totAvailable);
                if (totalWaterUptake > 0.0)
                {
                    wFrac = Math.Min(1.0, Math.Max(0.0, totalWaterUptake / totAvailable));
                    for (int layer = 0; layer <= swardRootZoneBottomLayer; layer++)
                    {
                        soilWaterUptake[layer] = soilAvailableWater[layer] * wFrac;
                        for (int s = 0; s < NumSpecies; s++)
                        {
                            sFrac = MathUtility.Divide(mySpecies[s].WaterDemand, swardWaterDemand, 0.0);
                            mySpecies[s].soilWaterUptake[layer] = soilWaterUptake[layer] * sFrac;
                        }
                    }
                }
                else
                {
                    for (int s = 0; s < NumSpecies; s++)
                    {
                        Array.Clear(mySpecies[s].soilWaterUptake, 0, nLayers);
                    }
                }
            }
        }
        else
        {
            // uptake was computed by external module (SWIM)
            // partition of uptake was already done in PlantWaterAvailability(), using PlantAvailableWaterAPSIM()
            // need to check here whether uptake is smaller than estimated by SWIM and return any excess water
            if (usingWUptakeBySpecies)
            {
                // consider each species
                double[] XSwater = new double[dlayer.Length];
                for (int layer = 0; layer <= swardRootZoneBottomLayer; layer++)
                {
                    for (int s = 0; s < NumSpecies; s++)
                    {
                        soilWaterUptake[layer] += mySpecies[s].soilWaterUptake[layer];
                        totalWaterUptake += soilWaterUptake[layer];

                        // check for excess of water estimated by SWIM
                        XSwater[layer] = swardWaterUptakeByAPSIM[layer] - soilWaterUptake[layer];
                    }
                }
                if (XSwater.Sum() > 0.0)
                {
                    SendWaterChanges(XSwater);
                    Console.WriteLine("AgPasture is sending " + XSwater.Sum().ToString("#0.00#") +
                                      " mm of water back to soil module (uptake not used by plant)");
                }
            }
            else
            {
                // consider only whole sward
                for (int s = 0; s < NumSpecies; s++)
                {
                    wFrac = MathUtility.Divide(mySpecies[s].WaterDemand, swardWaterDemand, 1.0);
                    for (int layer = 0; layer <= swardRootZoneBottomLayer; layer++)
                    {
                        soilWaterUptake[layer] = swardWaterUptakeByAPSIM[layer];
                        mySpecies[s].soilWaterUptake[layer] = soilWaterUptake[layer] * Math.Min(1.0, wFrac);
                        totalWaterUptake += soilWaterUptake[layer];
                    }
                }
            }
        }
    }

    /// <summary>Gets the amount of plant available water in the soil</summary>
    /// <returns>Amount of plant available soil water</returns>
    private double[] PlantWaterAvailability()
    {
        // clear some variables
        for (int s = 0; s < NumSpecies; s++)
            Array.Clear(mySpecies[s].soilAvailableWater, 0, dlayer.Length);

        if (WaterUptakeSource.ToLower() == "calc")
        {
            if (waterExtractabilityMethod == 1)
                return PlantAvailableSoilWaterAlternativeKL();
            else if (waterExtractabilityMethod == 2)
                return PlantAvailableSoilWaterAlternativeKS();
            else
                return PlantAvailableWaterDefaultAPSIM();
        }
        else
        {
            return PlantAvailableWaterAPSWIM();
        }
    }

    /// <summary>
    /// Get the amount of plant available soil water
    /// This method consider root presence in each layer and classic definition of kl,
    /// i.e., kl is the fraction of water available for uptake
    /// </summary>
    /// <returns>Amount of plant available water</returns>
    private double[] PlantAvailableWaterDefaultAPSIM()
    {
        double[] PAW = new double[dlayer.Length]; // total amount of Plant Available Water
        double layerFrac = 0.0; // fraction of layer explored by roots
        double auxAvailableWater = 0.0; // auxiliary amount of available water
        double potentialAvailableWater = 0.0; // potential (or maximum) amount of water available
        int nSpecies = 0; // number of species with root within a layer

        // find out plant soil available water
        if (usingWAvailableBySpecies)
        {
            // considering root presence for each species
            for (int layer = 0; layer <= swardRootZoneBottomLayer; layer++)
            {
                potentialAvailableWater = 0.0;
                nSpecies = 0;
                for (int s = 0; s < NumSpecies; s++)
                {
                    // amount if each species was alone
                    layerFrac = mySpecies[s].LayerFractionWithRoots(layer);
                    if (layerFrac > 0.0)
                    {
                        nSpecies += 1;
                        auxAvailableWater = Math.Max(0.0, (sw_dep[layer] - LL_dep[layer]) * layerFrac);
                        double xFac = Math.Min(1.0, kl[layer]);
                        potentialAvailableWater = Math.Max(potentialAvailableWater, auxAvailableWater);
                        mySpecies[s].soilAvailableWater[layer] = auxAvailableWater * xFac;
                        PAW[layer] += mySpecies[s].soilAvailableWater[layer];
                    }
                }

                // correct total PAW to make sure it doesn't exceed potential available
                auxAvailableWater = PAW[layer];
                PAW[layer] = Math.Min(PAW[layer], potentialAvailableWater);

                // correct values for each species to match PAW
                double wFrac = MathUtility.Divide(PAW[layer], auxAvailableWater, 0.0);
                if (wFrac < 1.0)
                {
                    for (int s = 0; s < NumSpecies; s++)
                        mySpecies[s].soilAvailableWater[layer] *= wFrac;
                }
            }
        }
        else
        {
            // considering the whole sward
            for (int layer = 0; layer <= swardRootZoneBottomLayer; layer++)
            {
                layerFrac = mySpecies[0].LayerFractionWithRoots(layer);
                auxAvailableWater = Math.Max(0.0, (sw_dep[layer] - LL_dep[layer]) * layerFrac);
                double xFac = Math.Min(1.0, kl[layer]);
                PAW[layer] = auxAvailableWater * xFac;
                for (int s = 0; s < NumSpecies; s++)
                {
                    // simple partition, based on demand
                    double wFrac = Math.Min(1.0, MathUtility.Divide(mySpecies[s].WaterDemand, swardWaterDemand, 0.0));
                    mySpecies[s].soilAvailableWater[layer] = PAW[layer] * wFrac;
                }
            }
        }

        return PAW;
    }

    /// <summary>
    /// Gets the amount of plant available soil water
    /// This method consider root distribution (density), plus water content with a new definition of kl.
    /// kl is reinterpreted as a factor describing the general water availability factor for each layer, being a soil only property.
    /// The combination of kl and relative soil water content describes how easy it is to uptake water (roughly related to water conductivity)
    /// </summary>
    /// <returns>Amount of plant available water</returns>
    private double[] PlantAvailableSoilWaterAlternativeKL()
    {
        double[] PAW = new double[dlayer.Length]; // total amount of Plant Available Water
        double layerFrac = 0.0; // fraction of layer explored by roots
        double auxAvailableWater = 0.0; // auxiliary amount of available water
        double potentialAvailableWater = 0.0; // potential (or maximum) amount of water available
        int nRootSpecies = 0; // number of species with root within a layer

        // find out plant soil available water
        if (usingWAvailableBySpecies)
        {
            // considering root presence for each species
            for (int layer = 0; layer <= swardRootZoneBottomLayer; layer++)
            {
                potentialAvailableWater = 0.0;
                nRootSpecies = 0;
                double swFac = MathUtility.Divide(sw_dep[layer] - LL_dep[layer], DUL_dep[layer] - LL_dep[layer], 0.0);
                swFac = MathUtility.Bound(swFac, 0.0, 1.0);
                for (int s = 0; s < NumSpecies; s++)
                {
                    // get water amount as if each species was alone
                    layerFrac = mySpecies[s].LayerFractionWithRoots(layer);
                    if (layerFrac > 0.0)
                    {
                        nRootSpecies += 1;
                        auxAvailableWater = Math.Max(0.0, (sw_dep[layer] - LL_dep[layer]) * layerFrac);
                        swFac = 1.0 - Math.Pow(1.0 - swFac, mySpecies[s].exponentSWCuptake);
                        double rldFac = MathUtility.Divide(mySpecies[s].RLD[layer], mySpecies[s].referenceRLD, 0.0);
                        double xFac = Math.Min(1.0, kl[layer] * swFac * rldFac);
                        potentialAvailableWater = Math.Max(potentialAvailableWater, auxAvailableWater);
                        mySpecies[s].soilAvailableWater[layer] = auxAvailableWater * xFac;
                        PAW[layer] += mySpecies[s].soilAvailableWater[layer];
                    }
                }

                // correct total PAW to make sure it doesn't exceed potential available
                auxAvailableWater = PAW[layer];
                PAW[layer] = Math.Min(PAW[layer], potentialAvailableWater);

                // correct values for each species to match PAW
                double wFrac = MathUtility.Divide(PAW[layer], auxAvailableWater, 0.0);
                if (wFrac < 1.0)
                {
                    for (int s = 0; s < NumSpecies; s++)
                        mySpecies[s].soilAvailableWater[layer] *= wFrac;
                }
            }
        }
        else
        {
            // considering the whole sward
            for (int layer = 0; layer <= swardRootZoneBottomLayer; layer++)
            {
                layerFrac = mySpecies[0].LayerFractionWithRoots(layer);
                auxAvailableWater = Math.Max(0.0, (sw_dep[layer] - LL_dep[layer]) * layerFrac);
                double swFac = MathUtility.Divide(sw_dep[layer] - LL_dep[layer], DUL_dep[layer] - LL_dep[layer], 0.0);
                swFac = MathUtility.Bound(swFac, 0.0, 1.0);
                swFac = 1.0 - Math.Pow(1.0 - swFac, ExponentSWCUptake[0]);
                double rldFac = MathUtility.Divide(rlv[layer], refRLD[0], 0.0);
                double xFac = Math.Min(1.0, kl[layer] * swFac * rldFac);
                PAW[layer] = auxAvailableWater * xFac;
                for (int s = 0; s < NumSpecies; s++)
                {
                    // partition based on root distribution
                    double wFrac = Math.Min(1.0, MathUtility.Divide(mySpecies[s].RLD[layer], rlv[layer], 0.0));
                    mySpecies[s].soilAvailableWater[layer] = PAW[layer] * wFrac;
                }
            }
        }

        return PAW;
    }

    /// <summary>
    /// Gets the amount of plant available soil water
    /// This method does not use kl, a relative Ksat is used instead. This is modified by water content plus root distribution (density)
    /// is also considered. Ksat and RLD are normalised using power function such that their effect is 90% at the reference value.
    /// The combination of kl and relative soil water content describes how easy it is to uptake water (roughly related to water conductivity)
    /// </summary>
    /// <returns>Amount of plant available water</returns>
    private double[] PlantAvailableSoilWaterAlternativeKS()
    {
        double[] PAW = new double[dlayer.Length]; // total amount of Plant Available Water
        double layerFrac = 0.0; // fraction of layer explored by roots
        double auxAvailableWater = 0.0; // auxiliary amount of available water
        double potentialAvailableWater = 0.0; // potential (or maximum) amount of water available
        int nRootSpecies = 0; // number of species with root within a layer

        // find out plant soil available water
        if (usingWAvailableBySpecies)
        {
            // considering root presence for each species
            for (int layer = 0; layer <= swardRootZoneBottomLayer; layer++)
            {
                potentialAvailableWater = 0.0;
                nRootSpecies = 0;
                double swFac = MathUtility.Divide(sw_dep[layer] - LL_dep[layer], DUL_dep[layer] - LL_dep[layer], 0.0);
                swFac = MathUtility.Bound(swFac, 0.0, 1.0);
                for (int s = 0; s < NumSpecies; s++)
                {
                    // get water amount as if each species was alone
                    layerFrac = mySpecies[s].LayerFractionWithRoots(layer);
                    if (layerFrac > 0.0)
                    {
                        nRootSpecies += 1;
                        double condFac = 1.0 - Math.Pow(10.0, -ks[layer] / ReferenceKSuptake[s]);
                        swFac = 1.0 - Math.Pow(1.0 - swFac, mySpecies[s].exponentSWCuptake);
                        auxAvailableWater = Math.Max(0.0, (sw_dep[layer] - LL_dep[layer]) * layerFrac);
                        double rldFac = 1.0 - Math.Pow(10.0, MathUtility.Divide(mySpecies[s].RLD[layer], mySpecies[s].referenceRLD, 0.0));
                        double xFac = Math.Min(1.0, condFac * swFac * rldFac);
                        potentialAvailableWater = Math.Max(potentialAvailableWater, auxAvailableWater);
                        mySpecies[s].soilAvailableWater[layer] = auxAvailableWater * xFac;
                        PAW[layer] += mySpecies[s].soilAvailableWater[layer];
                    }
                }

                // correct total PAW to make sure it doesn't exceed potential available
                auxAvailableWater = PAW[layer];
                PAW[layer] = Math.Min(PAW[layer], potentialAvailableWater);

                // correct values for each species to match PAW
                double wFrac = MathUtility.Divide(PAW[layer], auxAvailableWater, 0.0);
                if (wFrac < 1.0)
                {
                    for (int s = 0; s < NumSpecies; s++)
                        mySpecies[s].soilAvailableWater[layer] *= wFrac;
                }
            }
        }
        else
        {
            // considering the whole sward
            for (int layer = 0; layer <= swardRootZoneBottomLayer; layer++)
            {
                double condFac = 1.0 - Math.Pow(10.0, -ks[layer] / ReferenceKSuptake[0]);
                double swFac = MathUtility.Divide(sw_dep[layer] - LL_dep[layer], DUL_dep[layer] - LL_dep[layer], 0.0);
                swFac = MathUtility.Bound(swFac, 0.0, 1.0);
                swFac = 1.0 - Math.Pow(1.0 - swFac, ExponentSWCUptake[0]);
                double rldFac = 1.0 - Math.Pow(10.0, MathUtility.Divide(rlv[layer], refRLD[0], 0.0));

                layerFrac = mySpecies[0].LayerFractionWithRoots(layer);
                auxAvailableWater = Math.Max(0.0, (sw_dep[layer] - LL_dep[layer]) * layerFrac);
                double xFac = Math.Min(1.0, condFac * swFac * rldFac);
                PAW[layer] = auxAvailableWater * xFac;
                for (int s = 0; s < NumSpecies; s++)
                {
                    // partition based on root distribution
                    double wFrac = Math.Min(1.0, MathUtility.Divide(mySpecies[s].RLD[layer], rlv[layer], 0.0));
                    mySpecies[s].soilAvailableWater[layer] = PAW[layer] * wFrac;
                }
            }
        }

        return PAW;
    }

    /// <summary>
    /// Gets the amount of plant available/taken up soil water
    /// This method provides only an estimated partition, the actual uptake was already computed
    /// by other module (SWIM)
    /// </summary>
    /// <returns>Amount of plant available water</returns>
    private double[] PlantAvailableWaterAPSWIM()
    {
        double[] PAW = new double[dlayer.Length]; // total amount of Plant Available Water
        double wFrac = 0.0; // available fraction for each species

        // check that we have an input from apsim-swim
        if (swardWaterUptakeByAPSIM == null)
            throw new Exception(
                "No module provided an estimate for water uptake, check water module or set WaterUptakeSource to \"calc\"");

        // update/partition the uptake (from SWIM)
        if (usingWAvailableBySpecies)
        {
            // consider water available for each species, consider root distribution - this might mean that some water will go back to SWIM
            double[,] auxUptake = new double[NumSpecies, dlayer.Length];
            double[] auxTotalUptake = new double[NumSpecies];
            double[] auxActualUptake = new double[NumSpecies];
            double upWeight;
            double totWeight;

            // basic partition of uptake, based on root distribution and demand
            for (int layer = 0; layer <= swardRootZoneBottomLayer; layer++)
            {
                totWeight = mySpecies.Sum(x => x.WaterDemand * x.RLD[layer]);
                for (int s = 0; s < NumSpecies; s++)
                {
                    upWeight = mySpecies[s].WaterDemand * mySpecies[s].RLD[layer] / totWeight;
                    auxUptake[s, layer] = swardWaterUptakeByAPSIM[layer] * upWeight;
                    auxTotalUptake[s] += auxUptake[s, layer];
                }
            }

            double auxDemand;
            double uptakeUpToThisLayer;
            for (int s = 0; s < NumSpecies; s++)
            {
                auxDemand = mySpecies[s].WaterDemand * swardWaterUptakeByAPSIM.Sum() / swardWaterDemand;
                auxActualUptake[s] = Math.Max(Math.Min(auxDemand, auxTotalUptake[s]),
                    Math.Min(mySpecies[s].WaterDemand, auxTotalUptake[s]));
                uptakeUpToThisLayer = 0.0;
                for (int layer = 0; layer <= swardRootZoneBottomLayer; layer++)
                {
                    mySpecies[s].soilWaterUptake[layer] = Math.Min(auxUptake[s, layer], mySpecies[s].WaterDemand -
                                                                                        uptakeUpToThisLayer);
                    mySpecies[s].soilAvailableWater[layer] = mySpecies[s].soilWaterUptake[layer];
                    PAW[layer] += mySpecies[s].soilAvailableWater[layer];
                    uptakeUpToThisLayer += mySpecies[s].soilWaterUptake[layer];
                }
            }
        }
        else
        {
            // consider water available for whole sward
            for (int layer = 0; layer <= swardRootZoneBottomLayer; layer++)
            {
                for (int s = 0; s < NumSpecies; s++)
                {
                    // simple partition, based on demand
                    wFrac = MathUtility.Divide(mySpecies[s].WaterDemand, swardWaterDemand, 1.0);
                    mySpecies[s].soilWaterUptake[layer] = swardWaterUptakeByAPSIM[layer] * Math.Min(1.0, wFrac);
                    mySpecies[s].soilAvailableWater[layer] = mySpecies[s].soilWaterUptake[layer];
                    PAW[layer] += mySpecies[s].soilAvailableWater[layer];
                }
            }
        }

        return PAW;
    }

    /// <summary>Performs the actual changes in soil water due to plant uptake</summary>
    private void DoSoilWaterUptake()
    {
        if ((WaterUptakeSource.ToLower() == "calc") && (soilWaterUptake.Sum() > 0.0))
            SendWaterChanges(soilWaterUptake);
    }

    /// <summary>
    /// Send info about water changes to soil module
    /// </summary>
    /// <param name="WAmount">Delta water amount for each soil layer</param>
    private void SendWaterChanges(double[] WAmount)
    {
        // initialise water uptake data type
        WaterChangedType WaterUptake = new WaterChangedType();
        WaterUptake.DeltaWater = new double[dlayer.Length];

        // set the amounts to send
        for (int layer = 0; layer <= swardRootZoneBottomLayer; layer++)
            WaterUptake.DeltaWater[layer] = -WAmount[layer];

        if (WaterChanged != null)
            WaterChanged.Invoke(WaterUptake);
    }

    /// <summary>Responds to a WaterUptakesCalculated event</summary>
    /// <param name="SoilWater">WaterUptakesCalculated</param>
    [EventHandler]
    public void OnWaterUptakesCalculated(WaterUptakesCalculatedType SoilWater)
    {
        // Gets the water uptake for each layer as calculated by an external module (SWIM)
        for (int i_Crop = 0; i_Crop != SoilWater.Uptakes.Length; i_Crop++)
        {
            string MyName = SoilWater.Uptakes[i_Crop].Name;
            if (MyName == thisCropName)
            {
                swardWaterUptakeByAPSIM = new double[dlayer.Length];
                int length = SoilWater.Uptakes[i_Crop].Amount.Length;
                for (int layer = 0; layer < length; layer++)
                {
                    swardWaterUptakeByAPSIM[layer] = SoilWater.Uptakes[i_Crop].Amount[layer];
                }
            }
        }
    }

    /// <summary>Sets the soil moisture stress factor to each species</summary>
    /// <remarks>Worth more efforts in this area (F. Li)</remarks>
    private void SetSpeciesGLFWater()
    {
        if (swardWaterDemand == 0)
        {
            swardGLFWater = 1.0;
            for (int s = 0; s < NumSpecies; s++)
                mySpecies[s].glfWater = swardGLFWater;
        }
        else if (soilWaterUptake.Sum() == 0)
        {
            swardGLFWater = 0.0;
            for (int s = 0; s < NumSpecies; s++)
                mySpecies[s].glfWater = swardGLFWater;
        }
        else
        {
            if (usingWUptakeBySpecies)
            {
                double accum_gfwater = 0.0;
                for (int s = 0; s < NumSpecies; s++)
                {
                    mySpecies[s].glfWater = MathUtility.Divide(mySpecies[s].soilWaterUptake.Sum(),
                        mySpecies[s].WaterDemand, 1.0);
                    accum_gfwater += mySpecies[s].glfWater * mySpecies[s].greenLAI;
                }

                swardGLFWater = MathUtility.Divide(accum_gfwater, LAIGreen, 1.0);
            }
            else
            {
                swardGLFWater = MathUtility.Divide(soilWaterUptake.Sum(), swardWaterDemand, 1.0);

                // pass the glf to each species
                for (int s = 0; s < NumSpecies; s++)
                {
                    mySpecies[s].glfWater = swardGLFWater;
                }
            }
        }
    }

    /// <summary>Sets soil aeration stress factor to each species</summary>
    /// <remarks>Separated from GLFwater (RCichota, Dec/2015)</remarks>
    private void SetSpeciesGLFAeration()
    {
        double mySW = 0.0; //soil water content
        double mySat = 0.0; //water content at saturation
        double myMPL = 0.0; //water content for full aeration (approx. field capacity)
        double layerFrac = 1.0;
        double accum_glfair = 0.0;

        //if (swardGLFWater>0.999)
        //{
        if (usingWUptakeBySpecies)
        {
            for (int s = 0; s < NumSpecies; s++)
            {
                for (int layer = 0; layer <= mySpecies[s].layerBottomRootZone; layer++)
                {
                    layerFrac = mySpecies[s].LayerFractionWithRoots(layer);
                    mySW += sw_dep[layer] * layerFrac;
                    mySat += SAT_dep[layer] * layerFrac;
                    if (mySpecies[s].minMacroPorosity > 0.0)
                        myMPL += SAT_dep[layer] * (1.0 - mySpecies[s].minMacroPorosity) * layerFrac;
                    else
                        myMPL += DUL_dep[layer] * layerFrac;
                }

                if (mySW > myMPL)
                {
                    // soil close to saturation
                    mySpecies[s].glfAeration = 1.0 - (mySpecies[s].soilSatFactor * (mySW - myMPL) / (mySat - myMPL));
                }
                else
                    mySpecies[s].glfAeration = 1.0;

                accum_glfair += mySpecies[s].glfAeration * mySpecies[s].greenLAI;
            }

            swardGLFWLogging = MathUtility.Divide(accum_glfair, LAIGreen, 1.0);
        }
        else
        {
            for (int layer = 0; layer <= swardRootZoneBottomLayer; layer++)
            {
                layerFrac = mySpecies[0].LayerFractionWithRoots(layer);
                mySW += sw_dep[layer] * layerFrac;
                mySat += SAT_dep[layer] * layerFrac;
                if (mySpecies[0].minMacroPorosity > 0.0)
                    myMPL += SAT_dep[layer] * (1.0 - mySpecies[0].minMacroPorosity) * layerFrac;
                else
                    myMPL += DUL_dep[layer] * layerFrac;
            }

            if (mySW > myMPL)
            {
                // soil close to saturation
                swardGLFWLogging = 1.0 - (mySpecies[0].soilSatFactor * (mySW - myMPL) / (mySat - myMPL));
            }
            else
                swardGLFWLogging = 1.0;

            // pass the glf to each species
            for (int s = 0; s < NumSpecies; s++)
            {
                mySpecies[s].glfAeration = swardGLFWLogging;
            }
        }
    }

    #endregion  - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -

    #region - Nitrogen uptake processes - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -

    /// <summary>Checks the N budget and uptake processes</summary>
    private void DoNitrogenCalculations()
    {
        //1) Get soil N available in the root zone
        swardSoilNavailable = PlantNExtractability();

        //2) Get N fixation for legumes
        EvaluateNitrogenFixation();

        //3) Get N remobilised of senesced material and calculate N demand from soil
        EvaluateSoilNDemand();

        //4) Get the actual soil N uptake
        EvaluateSoilNitrogenUptake();

        //5) Consider remobilisation of luxury N
        EvaluateLuxuryNRemobilisation();

        //6) Compute partition of N uptake for each N form and layer
        PartitionNUptake();
    }

    /// <summary>Computes the amount of nitrogen demand for optimum N content as well as luxury uptake</summary>
    internal void EvaluateNitrogenDemand()
    {
        swardNdemandLux = 0.0;
        swardNdemandOpt = 0.0;
        for (int s = 0; s < NumSpecies; s++)
        {
            mySpecies[s].CalcTotalNDemand();
            swardNdemandOpt += mySpecies[s].NdemandOpt;
            swardNdemandLux += mySpecies[s].NdemandLux;
        }
    }

    /// <summary>Computes the amount of atmospheric nitrogen fixed through symbiosis</summary>
    internal void EvaluateNitrogenFixation()
    {
        swardNFixed = 0.0;
        if (usingNAvailableBySpecies)
        {
            // consider each species separately (need N available for each species)
            for (int s = 0; s < NumSpecies; s++)
            {
                if (mySpecies[s].isLegume)
                {
                    mySpecies[s].CalcNFixation();
                    swardNFixed += mySpecies[s].NFixed;
                }
            }
        }
        else
        {
            // get the minimum N fixation for whole sward
            for (int s = 0; s < NumSpecies; s++)
            {
                if (mySpecies[s].isLegume)
                {
                    mySpecies[s].NFixed = mySpecies[s].MinFix * mySpecies[s].NdemandOpt;
                    swardNFixed += mySpecies[s].NFixed;
                }
            }

            // consider additional fixation
            double Nstress = 1.0;
            if (swardNdemandOpt > 0.0 && (swardNdemandOpt > swardSoilNavailable + swardNFixed))
                Nstress = swardSoilNavailable / (swardNdemandOpt - swardNFixed);

            if (1.0 - Nstress > Epsilon)
            {
                // more fixation under N stress
                for (int s = 0; s < NumSpecies; s++)
                {
                    if (mySpecies[s].isLegume)
                    {
                        double moreNfixation = (mySpecies[s].MaxFix - mySpecies[s].MinFix) * (1.0 - Nstress);
                        moreNfixation = Math.Max(0.0, Math.Min(1.0, moreNfixation)) * mySpecies[s].NdemandOpt;
                        mySpecies[s].NFixed += moreNfixation;
                        swardNFixed += moreNfixation;
                    }
                }
            }
        }
    }

    /// <summary>Computes the amount of nitrogen demanded from the soil</summary>
    private void EvaluateSoilNDemand()
    {
        swardSoilNDemand = 0.0;
        for (int s = 0; s < NumSpecies; s++)
        {
            mySpecies[s].CalcNRemobSenescent();

            if (mySpecies[s].newGrowthN < mySpecies[s].NdemandLux)
            {
                // all Nremob and/or Nfix were used up, check demand from the soil
                mySpecies[s].soilNdemand = mySpecies[s].NdemandLux - mySpecies[s].newGrowthN;
                swardSoilNDemand += mySpecies[s].soilNdemand;
            }
            else
                mySpecies[s].soilNdemand = 0.0;
        }
    }

    /// <summary>
    /// Gets the amount of soil N that plants can extract
    /// </summary>
    /// <returns>Amount of N available to plants</returns>
    private double PlantNExtractability()
    {
        // clear some variables
        Array.Clear(soilNH4Available, 0, dlayer.Length);
        Array.Clear(soilNO3Available, 0, dlayer.Length);
        for (int s = 0; s < NumSpecies; s++)
        {
            Array.Clear(mySpecies[s].soilAvailableNH4, 0, dlayer.Length);
            Array.Clear(mySpecies[s].soilAvailableNO3, 0, dlayer.Length);
        }

        if (NUptakeSource.ToLower() == "calc")
        {
            if (NExtractabilityMethod == 1)
            {
                return PlantAvailableSoilNDefaultAPSIM();
            }
            if (NExtractabilityMethod == 2)
            {
                return PlantAvailableSoilNAlternativeRLD();
            }
            if (NExtractabilityMethod == 3)
            {
                return PlantAvailableSoilNAlternativeWup();
            }
            else // default method = 0
            {
                return PlantAvailableSoilNBasicAgPasture();
            }
        }
        else
        {
            return PlantExtractableSoilNApsim();
            // N uptake calculated by other modules (e.g., SWIM) - not implemented yet, will throw and error
        }
    }

    /// <summary>
    /// Gets the amount of soil N that plants can extract
    /// This is the 'classic' agpasture method, all N in the root zone is available
    /// </summary>
    /// <remarks>
    /// The method has been modified to consider the existence of SoilCNPatches in SoilNitrogen
    /// This implies using NN_PlantAvailable instead of NN (with NN being nh4 or no3). It means
    /// plants access is limited to patches with very high N content
    /// </remarks>
    /// <returns>Amount of N available to plants</returns>
    private double PlantAvailableSoilNBasicAgPasture()
    {
        double totalAvailable = 0.0;
        double layerFrac = 1.0; // fraction of each layer explored by roots
        double auxAvailableNH4; // auxiliary NH4 amount available
        double auxAvailableNO3; // auxiliary NO3 amount available
        double potentialAvailableNH4; // maximum NH4 amount available in each layer
        double potentialAvailableNO3; // maximum NO3 amount available in each layer
        double[] xFac = new double[2]; // extractability factor for each layer
        double nFrac = 0.0; // fraction available for each species

        if (usingNAvailableBySpecies)
        {
            // consider each species
            xFac[0] = 1.0;
            xFac[1] = 1.0;
            for (int layer = 0; layer <= swardRootZoneBottomLayer; layer++)
            {
                potentialAvailableNH4 = 0.0;
                potentialAvailableNO3 = 0.0;
                for (int s = 0; s < NumSpecies; s++)
                {
                    // amount as if each species was alone
                    layerFrac = mySpecies[s].LayerFractionWithRoots(layer);
                    if (layerFrac > 0.0)
                    {
                        if (nh4_PlantAvailable == null)
                        {
                            // there are no soilNPatches, use classic approach
                            auxAvailableNH4 = nh4[layer] * layerFrac;
                            auxAvailableNO3 = no3[layer] * layerFrac;
                        }
                        else
                        {
                            // SoilNitrogen has patches, best to use plant available
                            auxAvailableNH4 = Math.Min(nh4[layer], nh4_PlantAvailable[layer]) * layerFrac;
                            auxAvailableNO3 = Math.Min(no3[layer], no3_PlantAvailable[layer]) * layerFrac;
                        }

                        mySpecies[s].soilAvailableNH4[layer] = auxAvailableNH4 * xFac[0];
                        mySpecies[s].soilAvailableNO3[layer] = auxAvailableNO3 * xFac[1];
                        soilNH4Available[layer] += mySpecies[s].soilAvailableNH4[layer];
                        soilNO3Available[layer] += mySpecies[s].soilAvailableNO3[layer];
                        potentialAvailableNH4 = Math.Max(potentialAvailableNH4, auxAvailableNH4);
                        potentialAvailableNO3 = Math.Max(potentialAvailableNO3, auxAvailableNO3);
                    }
                }

                // adjust amounts of NH4 for each species
                nFrac = MathUtility.Divide(potentialAvailableNH4, soilNH4Available[layer], 0.0);
                if (nFrac < 0.999999)
                {
                    soilNH4Available[layer] = 0.0;
                    for (int s = 0; s < NumSpecies; s++)
                    {
                        mySpecies[s].soilAvailableNH4[layer] *= nFrac;
                        soilNH4Available[layer] += mySpecies[s].soilAvailableNH4[layer];
                    }
                }

                // adjust amounts of NO3 for each species
                nFrac = MathUtility.Divide(potentialAvailableNO3, soilNO3Available[layer], 0.0);
                if (nFrac < 0.999999)
                {
                    soilNO3Available[layer] = 0.0;
                    for (int s = 0; s < NumSpecies; s++)
                    {
                        mySpecies[s].soilAvailableNO3[layer] *= nFrac;
                        soilNO3Available[layer] += mySpecies[s].soilAvailableNO3[layer];
                    }
                }

                totalAvailable += soilNH4Available[layer] + soilNO3Available[layer];
            }
        }
        else
        {
            // consider whole sward
            xFac[0] = 1.0;
            xFac[1] = 1.0;
            for (int layer = 0; layer <= swardRootZoneBottomLayer; layer++)
            {
                layerFrac = mySpecies[0].LayerFractionWithRoots(layer);
                if (nh4_PlantAvailable == null)
                {
                    // there are no soilNPatches, use classic approach
                    auxAvailableNH4 = nh4[layer] * layerFrac;
                    auxAvailableNO3 = no3[layer] * layerFrac;
                }
                else
                {
                    // SoilNitrogen has patches, best to use plant available
                    auxAvailableNH4 = Math.Min(nh4[layer], nh4_PlantAvailable[layer]) * layerFrac;
                    auxAvailableNO3 = Math.Min(no3[layer], no3_PlantAvailable[layer]) * layerFrac;
                }

                soilNH4Available[layer] += auxAvailableNH4 * xFac[0];
                soilNO3Available[layer] += auxAvailableNO3 * xFac[1];

                // partition amount to each species (simple approach, based on root presence, values not really used)
                double totRootFrac = 0.0;
                for (int s = 0; s < NumSpecies; s++)
                    totRootFrac += mySpecies[s].LayerFractionWithRoots(layer);

                for (int s = 0; s < NumSpecies; s++)
                {
                    if (totRootFrac > 0.0)
                    {
                        nFrac = mySpecies[s].LayerFractionWithRoots(layer) / totRootFrac;
                        mySpecies[s].soilAvailableNH4[layer] = soilNH4Available[layer] * nFrac;
                        mySpecies[s].soilAvailableNO3[layer] = soilNO3Available[layer] * nFrac;
                    }
                    else
                    {
                        mySpecies[s].soilAvailableNH4[layer] = 0.0;
                        mySpecies[s].soilAvailableNO3[layer] = 0.0;
                    }
                }

                totalAvailable += soilNH4Available[layer] + soilNO3Available[layer];
            }
        }

        return totalAvailable;
    }

    /// <summary>
    /// Gets the amount of soil N that plants can extract (option 3 of Plant1)
    /// This approach considers soil moisture and N content, plus an extractability factor for each N form
    /// </summary>
    /// <remarks>
    /// This is an implementation of uptake option 3 of Plant1
    /// The method has been modified to consider the existence of SoilCNPatches in SoilNitrogen
    /// This implies using NN_PlantAvailable instead of NN (with NN being nh4 or no3). It means
    /// plants access is limited to patches with very high N content
    /// </remarks>
    /// <returns>Amount of N available to plants</returns>
    private double PlantAvailableSoilNDefaultAPSIM()
    {
        double totalAvailable = 0.0;
        double layerFrac = 1.0; // fraction of each layer explored by roots
        double auxAvailableNH4; // auxiliary NH4 amount available
        double auxAvailableNO3; // auxiliary NO3 amount available
        double potentialAvailableNH4; // maximum NH4 amount available in each layer
        double potentialAvailableNO3; // maximum NO3 amount available in each layer
        double MaxUptakeNH4 = 0.0; // maximum NH4 amount that can be taken up, in each layer
        double MaxUptakeNO3 = 0.0; // maximum NO3 amount that can be taken up, in each layer

        if (usingNAvailableBySpecies)
        {
            // consider each species
            for (int layer = 0; layer <= swardRootZoneBottomLayer; layer++)
            {
                potentialAvailableNH4 = 0.0;
                potentialAvailableNO3 = 0.0;
                double bdFac = 100.0 / (dlayer[layer] * bd[layer]);
                for (int s = 0; s < NumSpecies; s++)
                {
                    // amount as if each species was alone
                    layerFrac = mySpecies[s].LayerFractionWithRoots(layer);
                    if (layerFrac > 0.0)
                    {
                        double swFac = MathUtility.Divide(sw_dep[layer] - LL_dep[layer], DUL_dep[layer] - LL_dep[layer], 0.0);
                        swFac = MathUtility.Bound(swFac, 0.0, 1.0);
                        swFac = 1.0 - Math.Pow(1.0 - swFac, mySpecies[s].exponentSWCuptake);
                        MaxUptakeNH4 = mySpecies[s].MaximumUptakeRateNH4 * 0.01 * bd[layer] * dlayer[layer];
                        MaxUptakeNO3 = mySpecies[s].MaximumUptakeRateNO3 * 0.01 * bd[layer] * dlayer[layer];
                        if (nh4_PlantAvailable == null)
                        {
                            // there are no soilNPatches, use classic approach
                            auxAvailableNH4 = nh4[layer] * layerFrac;
                            auxAvailableNO3 = no3[layer] * layerFrac;
                        }
                        else
                        {
                            // SoilNitrogen has patches, best to use plant available
                            auxAvailableNH4 = Math.Min(nh4[layer], nh4_PlantAvailable[layer]) * layerFrac;
                            auxAvailableNO3 = Math.Min(no3[layer], no3_PlantAvailable[layer]) * layerFrac;
                        }

                        double xFac = Math.Min(1.0, bdFac * kNH4[layer] * swFac);
                        mySpecies[s].soilAvailableNH4[layer] = Math.Min(MaxUptakeNH4, Math.Pow(auxAvailableNH4, 2.0) * xFac);
                        xFac = Math.Min(1.0, bdFac * kNO3[layer] * swFac);
                        mySpecies[s].soilAvailableNO3[layer] = Math.Min(MaxUptakeNO3, Math.Pow(auxAvailableNO3, 2.0) * xFac);
                        soilNH4Available[layer] += mySpecies[s].soilAvailableNH4[layer];
                        soilNO3Available[layer] += mySpecies[s].soilAvailableNO3[layer];
                        potentialAvailableNH4 = Math.Max(potentialAvailableNH4, auxAvailableNH4);
                        potentialAvailableNO3 = Math.Max(potentialAvailableNO3, auxAvailableNO3);
                    }
                }

                // adjust amounts of NH4 for each species
                double nFrac = MathUtility.Divide(potentialAvailableNH4, soilNH4Available[layer], 0.0);
                if (nFrac < 0.999999)
                {
                    soilNH4Available[layer] = 0.0;
                    for (int s = 0; s < NumSpecies; s++)
                    {
                        mySpecies[s].soilAvailableNH4[layer] *= nFrac;
                        soilNH4Available[layer] += mySpecies[s].soilAvailableNH4[layer];
                    }
                }

                // adjust amounts of NO3 for each species
                nFrac = MathUtility.Divide(potentialAvailableNO3, soilNO3Available[layer], 0.0);
                if (nFrac < 0.999999)
                {
                    soilNO3Available[layer] = 0.0;
                    for (int s = 0; s < NumSpecies; s++)
                    {
                        mySpecies[s].soilAvailableNO3[layer] *= nFrac;
                        soilNO3Available[layer] += mySpecies[s].soilAvailableNO3[layer];
                    }
                }

                totalAvailable += soilNH4Available[layer] + soilNO3Available[layer];
            }
        }
        else
        {
            // consider whole sward
            for (int layer = 0; layer <= swardRootZoneBottomLayer; layer++)
            {
                double bdFac = 100.0 / (dlayer[layer] * bd[layer]);
                layerFrac = mySpecies[0].LayerFractionWithRoots(layer);
                double swFac = MathUtility.Divide(sw_dep[layer] - LL_dep[layer], DUL_dep[layer] - LL_dep[layer], 0.0);
                swFac = MathUtility.Bound(swFac, 0.0, 1.0);
                swFac = 1.0 - Math.Pow(1.0 - swFac, ExponentSWCUptake[0]);
                MaxUptakeNH4 = MaximumUptakeRateNH4[0] * 0.01 * bd[layer] * dlayer[layer];
                MaxUptakeNO3 = MaximumUptakeRateNO3[0] * 0.01 * bd[layer] * dlayer[layer];
                if (nh4_PlantAvailable == null)
                {
                    // there are no soilNPatches, use classic approach
                    auxAvailableNH4 = nh4[layer] * layerFrac;
                    auxAvailableNO3 = no3[layer] * layerFrac;
                }
                else
                {
                    // SoilNitrogen has patches, best to use plant available
                    auxAvailableNH4 = Math.Min(nh4[layer], nh4_PlantAvailable[layer]) * layerFrac;
                    auxAvailableNO3 = Math.Min(no3[layer], no3_PlantAvailable[layer]) * layerFrac;
                }

                double xFac = Math.Min(1.0, bdFac * kNH4[layer] * swFac);
                soilNH4Available[layer] += Math.Min(MaxUptakeNH4, Math.Pow(auxAvailableNH4, 2.0) * xFac);
                xFac = Math.Min(1.0, bdFac * kNO3[layer] * swFac);
                soilNO3Available[layer] += Math.Min(MaxUptakeNO3, Math.Pow(auxAvailableNO3, 2.0) * xFac);

                // partition amount to each species (simple approach, based on root length density)
                for (int s = 0; s < NumSpecies; s++)
                {
                    double nFrac = MathUtility.Divide(mySpecies[s].RLD[layer], rlv[layer], 0.0);
                    mySpecies[s].soilAvailableNH4[layer] = soilNH4Available[layer] * nFrac;
                    mySpecies[s].soilAvailableNO3[layer] = soilNO3Available[layer] * nFrac;
                }

                totalAvailable += soilNH4Available[layer] + soilNO3Available[layer];
            }
        }

        return totalAvailable;
    }

    /// <summary>
    /// Gets the amount of soil N that plants can extract
    /// This approach considers root distribution, water content and an extractability factor for each N form
    /// </summary>
    /// <remarks>
    /// The method has been modified to consider the existence of SoilCNPatches in SoilNitrogen
    /// This implies using NN_PlantAvailable instead of NN (with NN being nh4 or no3). It means
    /// plants access is limited to patches with very high N content
    /// </remarks>
    /// <returns>Amount of N available to plants</returns>
    private double PlantAvailableSoilNAlternativeRLD()
    {
        double totalAvailable = 0.0;
        double layerFrac = 1.0; // fraction of each layer explored by roots
        double auxAvailableNH4; // auxiliary NH4 amount available
        double auxAvailableNO3; // auxiliary NO3 amount available
        double potentialAvailableNH4; // maximum NH4 amount available in each layer
        double potentialAvailableNO3; // maximum NO3 amount available in each layer

        if (usingNAvailableBySpecies)
        {
            // consider each species
            for (int layer = 0; layer <= swardRootZoneBottomLayer; layer++)
            {
                potentialAvailableNH4 = 0.0;
                potentialAvailableNO3 = 0.0;
                for (int s = 0; s < NumSpecies; s++)
                {
                    //amount as if each species was alone
                    layerFrac = mySpecies[s].LayerFractionWithRoots(layer);
                    if (layerFrac > 0.0)
                    {
                        double swFac = MathUtility.Divide(sw_dep[layer] - LL_dep[layer], DUL_dep[layer] - LL_dep[layer], 0.0);
                        swFac = MathUtility.Bound(swFac, 0.0, 1.0);
                        swFac = 1.0 - Math.Pow(1.0 - swFac, mySpecies[s].exponentSWCuptake);
                        double rldFac = Math.Min(1.0, MathUtility.Divide(mySpecies[s].RLD[layer], mySpecies[s].referenceRLD, 1.0));
                        if (nh4_PlantAvailable == null)
                        {
                            // there are no soilNPatches, use classic approach
                            auxAvailableNH4 = nh4[layer] * layerFrac;
                            auxAvailableNO3 = no3[layer] * layerFrac;
                        }
                        else
                        {
                            // SoilNitrogen has patches, best to use plant available
                            auxAvailableNH4 = Math.Min(nh4[layer], nh4_PlantAvailable[layer]) * layerFrac;
                            auxAvailableNO3 = Math.Min(no3[layer], no3_PlantAvailable[layer]) * layerFrac;
                        }

                        double xFac = Math.Min(1.0, kNH4[layer] * swFac * rldFac);
                        mySpecies[s].soilAvailableNH4[layer] = auxAvailableNH4 * xFac;
                        xFac = Math.Min(1.0, kNO3[layer] * swFac * rldFac);
                        mySpecies[s].soilAvailableNO3[layer] = auxAvailableNO3 * xFac;
                        soilNH4Available[layer] += mySpecies[s].soilAvailableNH4[layer];
                        soilNO3Available[layer] += mySpecies[s].soilAvailableNO3[layer];
                        potentialAvailableNH4 = Math.Max(potentialAvailableNH4, auxAvailableNH4);
                        potentialAvailableNO3 = Math.Max(potentialAvailableNO3, auxAvailableNO3);
                    }
                }

                // adjust amounts of NH4 for each species
                double nFrac = MathUtility.Divide(potentialAvailableNH4, soilNH4Available[layer], 0.0);
                if (nFrac < 0.999999)
                {
                    soilNH4Available[layer] = 0.0;
                    for (int s = 0; s < NumSpecies; s++)
                    {
                        mySpecies[s].soilAvailableNH4[layer] *= nFrac;
                        soilNH4Available[layer] += mySpecies[s].soilAvailableNH4[layer];
                    }
                }

                // adjust amounts of NO3 for each species
                nFrac = MathUtility.Divide(potentialAvailableNO3, soilNO3Available[layer], 0.0);
                if (nFrac < 0.999999)
                {
                    soilNO3Available[layer] = 0.0;
                    for (int s = 0; s < NumSpecies; s++)
                    {
                        mySpecies[s].soilAvailableNO3[layer] *= nFrac;
                        soilNO3Available[layer] += mySpecies[s].soilAvailableNO3[layer];
                    }
                }

                totalAvailable += soilNH4Available[layer] + soilNO3Available[layer];
            }
        }
        else
        {
            // consider whole sward
            for (int layer = 0; layer <= swardRootZoneBottomLayer; layer++)
            {
                layerFrac = mySpecies[0].LayerFractionWithRoots(layer);
                double swFac = MathUtility.Divide(sw_dep[layer] - LL_dep[layer], DUL_dep[layer] - LL_dep[layer], 0.0);
                swFac = MathUtility.Bound(swFac, 0.0, 1.0);
                swFac = 1.0 - Math.Pow(1.0 - swFac, ExponentSWCUptake[0]);
                double rldFac = MathUtility.Divide(rlv[layer], refRLD[0], 1.0);
                if (nh4_PlantAvailable == null)
                {
                    // there are no soilNPatches, use classic approach
                    auxAvailableNH4 = nh4[layer] * layerFrac;
                    auxAvailableNO3 = no3[layer] * layerFrac;
                }
                else
                {
                    // SoilNitrogen has patches, best to use plant available
                    auxAvailableNH4 = Math.Min(nh4[layer], nh4_PlantAvailable[layer]) * layerFrac;
                    auxAvailableNO3 = Math.Min(no3[layer], no3_PlantAvailable[layer]) * layerFrac;
                }

                double xFac = Math.Min(1.0, kNH4[layer] * swFac * rldFac);
                soilNH4Available[layer] += auxAvailableNH4 * xFac;
                xFac = Math.Min(1.0, kNO3[layer] * swFac * rldFac);
                soilNO3Available[layer] += auxAvailableNO3 * xFac;

                // partition amount to each species (simple approach, based on root length density)
                for (int s = 0; s < NumSpecies; s++)
                {
                    double nFrac = MathUtility.Divide(mySpecies[s].RLD[layer], rlv[layer], 0.0);
                    mySpecies[s].soilAvailableNH4[layer] = soilNH4Available[layer] * nFrac;
                    mySpecies[s].soilAvailableNO3[layer] = soilNO3Available[layer] * nFrac;
                }

                totalAvailable += soilNH4Available[layer] + soilNO3Available[layer];
            }
        }

        return totalAvailable;
    }

    /// <summary>
    /// Gets the amount of soil N that plants can extract (method 6)
    /// This approach considers water uptake as the main factor, plus extractability factor for each N form 
    /// </summary>
    /// <remarks>
    /// The method has been modified to consider the existence of SoilCNPatches in SoilNitrogen
    /// This implies using NN_PlantAvailable instead of NN (with NN being nh4 or no3). It means
    /// plants access is limited to patches with very high N content
    /// </remarks>
    /// <returns>Amount of N available to plants</returns>
    private double PlantAvailableSoilNAlternativeWup()
    {
        double totalAvailable = 0.0;
        double layerFrac = 1.0; // fraction of each layer explored by roots
        double auxAvailableNH4; // auxiliary NH4 amount available
        double auxAvailableNO3; // auxiliary NO3 amount available
        double potentialAvailableNH4; // maximum NH4 amount available in each layer
        double potentialAvailableNO3; // maximum NO3 amount available in each layer
        double MaxUptakeNH4 = 0.0; // maximum NH4 amount that can be taken up, in each layer
        double MaxUptakeNO3 = 0.0; // maximum NO3 amount that can be taken up, in each layer

        if (usingNAvailableBySpecies)
        {
            // consider each species
            for (int layer = 0; layer <= swardRootZoneBottomLayer; layer++)
            {
                potentialAvailableNH4 = 0.0;
                potentialAvailableNO3 = 0.0;
                double swUpFactor = MathUtility.Divide(soilWaterUptake[layer], sw_dep[layer], 0.0);
                for (int s = 0; s < NumSpecies; s++)
                {
                    //amount as if each species was alone
                    layerFrac = mySpecies[0].LayerFractionWithRoots(layer);
                    if (layerFrac > 0.0)
                    {
                        MaxUptakeNH4 = mySpecies[s].MaximumUptakeRateNH4 * 0.01 * bd[layer] * dlayer[layer];
                        MaxUptakeNO3 = mySpecies[s].MaximumUptakeRateNO3 * 0.01 * bd[layer] * dlayer[layer];
                        double spciesF = MathUtility.Divide(mySpecies[s].soilWaterUptake[layer], soilWaterUptake[layer], 0.0);
                        if (nh4_PlantAvailable == null)
                        {
                            // there are no soilNPatches, use classic approach
                            auxAvailableNH4 = nh4[layer] * layerFrac;
                            auxAvailableNO3 = no3[layer] * layerFrac;
                        }
                        else
                        {
                            // SoilNitrogen has patches, best to use plant available
                            auxAvailableNH4 = nh4_PlantAvailable[layer] * layerFrac;
                            auxAvailableNO3 = no3_PlantAvailable[layer] * layerFrac;
                        }

                        double xFac = Math.Min(1.0, swUpFactor * kNH4[layer]);
                        mySpecies[s].soilAvailableNH4[layer] = Math.Min(MaxUptakeNH4, auxAvailableNH4 * xFac) * spciesF;
                        xFac = Math.Min(1.0, swUpFactor * kNO3[layer]);
                        mySpecies[s].soilAvailableNO3[layer] = Math.Min(MaxUptakeNO3, auxAvailableNO3 * xFac) * spciesF;
                        soilNH4Available[layer] += mySpecies[s].soilAvailableNH4[layer];
                        soilNO3Available[layer] += mySpecies[s].soilAvailableNO3[layer];
                        potentialAvailableNH4 = Math.Max(potentialAvailableNH4, auxAvailableNH4);
                        potentialAvailableNO3 = Math.Max(potentialAvailableNO3, auxAvailableNO3);
                    }
                }

                // adjust amounts of NH4 for each species
                double nFrac = Math.Min(1.0, MathUtility.Divide(potentialAvailableNH4, soilNH4Available[layer], 0.0));
                if (nFrac < 1.0)
                {
                    soilNH4Available[layer] = 0.0;
                    for (int s = 0; s < NumSpecies; s++)
                    {
                        mySpecies[s].soilAvailableNH4[layer] *= nFrac;
                        soilNH4Available[layer] += mySpecies[s].soilAvailableNH4[layer];
                    }
                }

                // adjust amounts of NO3 for each species
                nFrac = Math.Min(1.0, MathUtility.Divide(potentialAvailableNO3, soilNO3Available[layer], 0.0));
                if (nFrac < 1.0)
                {
                    soilNO3Available[layer] = 0.0;
                    for (int s = 0; s < NumSpecies; s++)
                    {
                        mySpecies[s].soilAvailableNO3[layer] *= nFrac;
                        soilNO3Available[layer] += mySpecies[s].soilAvailableNO3[layer];
                    }
                }

                totalAvailable += soilNH4Available[layer] + soilNO3Available[layer];
            }
        }
        else
        {
            // consider whole sward
            for (int layer = 0; layer <= swardRootZoneBottomLayer; layer++)
            {
                layerFrac = mySpecies[0].LayerFractionWithRoots(layer);
                double swUpFactor = MathUtility.Divide(soilWaterUptake[layer], soilAvailableWater[layer], 0.0);
                MaxUptakeNH4 = MaximumUptakeRateNH4[0] * 0.01 * bd[layer] * dlayer[layer];
                MaxUptakeNO3 = MaximumUptakeRateNO3[0] * 0.01 * bd[layer] * dlayer[layer];
                if (nh4_PlantAvailable == null)
                {
                    // there are no soilNPatches, use classic approach
                    auxAvailableNH4 = nh4[layer] * layerFrac;
                    auxAvailableNO3 = no3[layer] * layerFrac;
                }
                else
                {
                    // SoilNitrogen has patches, best to use plant available
                    auxAvailableNH4 = Math.Min(nh4[layer], nh4_PlantAvailable[layer]) * layerFrac;
                    auxAvailableNO3 = Math.Min(no3[layer], no3_PlantAvailable[layer]) * layerFrac;
                }

                double xFac = Math.Min(1.0, swUpFactor * kNH4[layer]);
                soilNH4Available[layer] += Math.Min(MaxUptakeNH4, auxAvailableNH4 * xFac);
                xFac = Math.Min(1.0, swUpFactor * kNO3[layer]);
                soilNO3Available[layer] += Math.Min(MaxUptakeNO3, auxAvailableNO3 * xFac);
            }

            for (int layer = 0; layer <= swardRootZoneBottomLayer; layer++)
            {
                // partition amount to each species (simple approach, based on water taken up, values not really used)
                for (int s = 0; s < NumSpecies; s++)
                {
                    double nFrac = MathUtility.Divide(mySpecies[s].soilWaterUptake[layer], soilWaterUptake[layer], 0.0);
                    mySpecies[s].soilAvailableNH4[layer] = soilNH4Available[layer] * nFrac;
                    mySpecies[s].soilAvailableNO3[layer] = soilNO3Available[layer] * nFrac;
                }

                totalAvailable += soilNH4Available[layer] + soilNO3Available[layer];
            }
        }

        return totalAvailable;
    }

    /// <summary>
    /// Gets the amount of soil N that plants can extract, or have extracted
    /// This approach does not actually computes extractable N, only collects the values
    /// it has been computed somewhere else (SoilNitrogen) for each N form
    /// </summary>
    /// <returns>Amount of N available to plants</returns>
    private double PlantExtractableSoilNApsim()
    {
        // check that we have an input from apsim
        if (swardWaterUptakeByAPSIM == null)
            throw new Exception(
                "No module provided an estimate for N uptake, check SoilN module or set NUptakeSource to \"calc\"");

        if (usingNAvailableBySpecies)
        {
            // consider each species  -  Not implemented
            throw new Exception("Procedure not implemented");
        }
        else
        {
            // consider whole sward
            double totalAvailable = 0.0;
            double nFrac = 0.0;
            for (int layer = 0; layer <= swardRootZoneBottomLayer; layer++)
            {
                soilNH4Available[layer] = swardNH4UptakeByAPSIM[layer];
                soilNO3Available[layer] = swardNO3UptakeByAPSIM[layer];

                // partition amount to each species (simple approach, values not really used)
                for (int s = 0; s < NumSpecies; s++)
                {
                    // based on root DM
                    nFrac = MathUtility.Divide(mySpecies[s].roots.DMGreen, RootWt, 0.0);
                    mySpecies[s].soilAvailableNH4[layer] = soilNH4Available[layer] * nFrac;
                    mySpecies[s].soilAvailableNO3[layer] = soilNO3Available[layer] * nFrac;
                }

                totalAvailable += soilNH4Available[layer] + soilNO3Available[layer];
            }
            return totalAvailable;
        }
    }

    /// <summary>Computes the amount of nitrogen taken up by the plant</summary>
    private void EvaluateSoilNitrogenUptake()
    {
        for (int s = 0; s < NumSpecies; s++)
        {
            if (mySpecies[s].soilNdemand < Epsilon)
            {
                // no need for uptake or extra remobilisation
                mySpecies[s].soilNH4Uptake = 0.0;
                mySpecies[s].soilNO3Uptake = 0.0;
            }
            else
            {
                if (usingAlternativeNUptake)
                {
                    // consider each species separately
                    mySpecies[s].CalcNUptake();
                }
                else
                {
                    // consider the whole sward
                    double nFormFrac = MathUtility.Divide(soilNH4Available.Sum(), (soilNH4Available.Sum() + soilNO3Available.Sum()), 0.0);
                    if (swardSoilNavailable >= swardSoilNDemand)
                    {
                        // soil can supply all N demanded for maximum uptake (luxury N)
                        mySpecies[s].soilNH4Uptake = mySpecies[s].soilNdemand * nFormFrac;
                        mySpecies[s].soilNO3Uptake = mySpecies[s].soilNdemand * (1.0 - nFormFrac);
                        mySpecies[s].newGrowthN += mySpecies[s].soilNH4Uptake + mySpecies[s].soilNO3Uptake;
                    }
                    else
                    {
                        // soil cannot supply all N needed. Uptake the available N and partition it between species
                        double speciesNuptake = swardSoilNavailable * MathUtility.Divide(mySpecies[s].soilNdemand, swardSoilNDemand, 0.0);
                        mySpecies[s].soilNH4Uptake = speciesNuptake * nFormFrac;
                        mySpecies[s].soilNO3Uptake = speciesNuptake * (1 - nFormFrac);
                        mySpecies[s].newGrowthN += mySpecies[s].soilNH4Uptake + mySpecies[s].soilNO3Uptake;
                    }
                }
            }
        }
    }

    /// <summary>Computes the amount of luxury N remobilised into new growth</summary>
    private void EvaluateLuxuryNRemobilisation()
    {
        for (int s = 0; s < NumSpecies; s++)
        {
            if (mySpecies[s].soilNdemand < Epsilon)
                mySpecies[s].NLuxury2NewGrowth = 0.0;
            else
            {
                if (usingAlternativeNUptake)
                {
                    // consider each species separately
                    // check whether demand for optimum growth has been satisfied
                    if (mySpecies[s].NdemandOpt > mySpecies[s].newGrowthN)
                    {
                        // plant still needs more N for optimum growth, check whether luxury N already in the plants can be used
                        mySpecies[s].CalcNRemobLuxury();
                    }
                    else
                    {
                        // N supply was enough for optimum growth, no need to use luxury N
                        mySpecies[s].NLuxury2NewGrowth = 0.0;
                    }
                }
                else
                {
                    // consider the whole sward
                    if (swardSoilNavailable >= swardSoilNDemand)
                    {
                        // soil can supply all N demanded for maximum uptake (luxury N)
                        mySpecies[s].NLuxury2NewGrowth = 0.0;
                    }
                    else
                    {
                        // soil cannot supply all N needed. Uptake the available N and partition it between species
                        // check whether demand for optimum growth has been satisfied
                        if (mySpecies[s].NdemandOpt > mySpecies[s].newGrowthN)
                        {
                            // plant still needs more N for optimum growth, check whether luxury N already in the plants can be used
                            mySpecies[s].CalcNRemobLuxury();
                        }
                        else
                        {
                            // N supply is enough for optimum growth, although luxury uptake is not fully accomplished
                            mySpecies[s].NLuxury2NewGrowth = 0.0;
                        }
                    }
                }
            }
        }
    }

    /// <summary>Evaluates the amount of N taken up from each layer, for each N form</summary>
    private void PartitionNUptake()
    {
        // clear some variables
        Array.Clear(soilNH4Uptake, 0, dlayer.Length);
        Array.Clear(soilNO3Uptake, 0, dlayer.Length);
        double upFrac;
        double totalNavailable;
        double totalNUptake;

        if (usingAlternativeNUptake)
        {
            // consider each species separately, aggregate amount here
            upFrac = 0.0;
            for (int s = 0; s < NumSpecies; s++)
            {
                totalNavailable = mySpecies[s].soilAvailableNH4.Sum() + mySpecies[s].soilAvailableNO3.Sum();
                totalNUptake = mySpecies[s].soilNH4Uptake + mySpecies[s].soilNO3Uptake;
                if (totalNUptake > 0.0)
                {
                    // there is some uptake
                    // partition amongst layers
                    upFrac = Math.Min(1.0, MathUtility.Divide(totalNUptake, totalNavailable, 0.0));
                    for (int layer = 0; layer <= mySpecies[s].layerBottomRootZone; layer++)
                    {
                        soilNH4Uptake[layer] += mySpecies[s].soilAvailableNH4[layer] * upFrac;
                        soilNO3Uptake[layer] += mySpecies[s].soilAvailableNO3[layer] * upFrac;
                    }
                }
            }
        }
        else
        {
            // consider the whole sward
            totalNavailable = swardSoilNavailable;
            totalNUptake = mySpecies.Sum(x => x.soilNH4Uptake + x.soilNO3Uptake);
            if (totalNUptake > 0.0)
            {
                // partition uptake amongst layers
                upFrac = Math.Min(1.0, MathUtility.Divide(totalNUptake, totalNavailable, 0.0));
                for (int layer = 0; layer <= swardRootZoneBottomLayer; layer++)
                {
                    soilNH4Uptake[layer] = soilNH4Available[layer] * upFrac;
                    soilNO3Uptake[layer] = soilNO3Available[layer] * upFrac;
                }
            }
        }

        // check that amounts to remove aren't larger that available
        for (int layer = 0; layer <= swardRootZoneBottomLayer; layer++)
        {
            if ((soilNH4Uptake[layer] - nh4[layer]) > Epsilon)
                throw new Exception("Loss of mass balance - NH4 uptake too large");

            if ((soilNO3Uptake[layer] - no3[layer]) > Epsilon)
                throw new Exception("Loss of mass balance - NO3 uptake too large");
        }
    }

    /// <summary>Performs the actual changes in soil N content due to plant uptake</summary>
    private void DoSoilNUptake()
    {
        if ((NUptakeSource.ToLower() == "calc") && (soilNH4Uptake.Sum() + soilNO3Uptake.Sum() > 0.0))
        {
            if ((NExtractabilityMethod == 4) && (PatchArea != null))
            {
                // uptake should be 'patch-aware' (to use when dealing with SoilCNPatches)
                int nPatches = PatchNH4.Patch.Length;
                double layerFrac = 0.0;
                double patchFrac = 0.0;
                double MaxNUptakeLayer = 0.0;
                double AvailableNLayer = 0.0;
                double[] patchUptakeNH4 = new double[dlayer.Length];
                double[] patchUptakeNO3 = new double[dlayer.Length];
                double TotalUptake = 0.0;
                for (int k = 0; k < nPatches; k++)
                {
                    // partition for each patch
                    for (int layer = 0; layer < dlayer.Length; layer++)
                    {
                        layerFrac = mySpecies[0].LayerFractionWithRoots(layer);
                        patchUptakeNH4[layer] = 0.0;
                        patchUptakeNO3[layer] = 0.0;
                        if (soilNH4Available[layer] > 0.0)
                        {
                            MaxNUptakeLayer = MaximumUptakeRateNH4[0] * 0.01 * bd[layer] * dlayer[layer];
                            AvailableNLayer = Math.Min(PatchNH4.Patch[k].Value[layer], MaxNUptakeLayer) * PatchArea[k] *
                                              layerFrac;
                            patchFrac = MathUtility.Divide(AvailableNLayer, soilNH4Available[layer], 0.0);
                            patchUptakeNH4[layer] = -(soilNH4Uptake[layer] * patchFrac) / PatchArea[k];
                        }
                        if (soilNO3Available[layer] > 0.0)
                        {
                            MaxNUptakeLayer = MaximumUptakeRateNO3[0] * 0.01 * bd[layer] * dlayer[layer];
                            AvailableNLayer = Math.Min(PatchNO3.Patch[k].Value[layer], MaxNUptakeLayer) * PatchArea[k] *
                                              layerFrac;
                            patchFrac = MathUtility.Divide(AvailableNLayer, soilNO3Available[layer], 0.0);
                            patchUptakeNO3[layer] = -(soilNO3Uptake[layer] * patchFrac) / PatchArea[k];
                        }
                    }
                    // send the N changes
                    SendNitrogenChangesByPatch(k, patchUptakeNH4, patchUptakeNO3);
                    TotalUptake += (patchUptakeNH4.Sum() + patchUptakeNO3.Sum()) * PatchArea[k];
                }

                // check that mass balance was kept
                if (Math.Abs(soilNH4Uptake.Sum() + soilNO3Uptake.Sum() + TotalUptake) > Epsilon)
                    throw new Exception("Loss of mass balance - partition between SoilCNPatches");
            }
            else
            {
                SendNitrogenChanges(soilNH4Uptake, soilNO3Uptake);
            }
        }
    }

    /// <summary>Sends info about N changes to soil module</summary>
    /// <param name="NH4Amount">Delta NH4 amount for each soil layer</param>
    /// <param name="NO3Amount">Delta NO3 amount for each soil layer</param>
    private void SendNitrogenChanges(double[] NH4Amount, double[] NO3Amount)
    {
        // initialise water uptake data type
        NitrogenChangedType NUptake = new NitrogenChangedType();
        NUptake.Sender = thisCropName;
        NUptake.SenderType = "Plant";
        NUptake.DeltaNO3 = new double[dlayer.Length];
        NUptake.DeltaNH4 = new double[dlayer.Length];

        // set the amounts to send
        for (int layer = 0; layer <= swardRootZoneBottomLayer; layer++)
        {
            NUptake.DeltaNH4[layer] = -NH4Amount[layer];
            NUptake.DeltaNO3[layer] = -NO3Amount[layer];
        }

        if (NitrogenChanged != null)
            NitrogenChanged.Invoke(NUptake);
    }

    /// <summary>Send info about N changes to soil module (consider SoilCNPatches)</summary>
    /// <param name="PatchToAddTo">Patch that will get this change</param>
    /// <param name="NH4Amount">Amount of NH4 change, for each layer</param>
    /// <param name="NO3Amount">Amount of NO3 change, for each layer</param>
    private void SendNitrogenChangesByPatch(int PatchToAddTo, double[] NH4Amount, double[] NO3Amount)
    {
        AddSoilCNPatchType PatchData = new AddSoilCNPatchType();
        PatchData.Sender = "AgPasture";
        PatchData.DepositionType = "ToSpecificPatch";
        PatchData.AreaFraction = 1.0;
        PatchData.AffectedPatches_id = new int[] {PatchToAddTo};
        PatchData.NH4 = NH4Amount;
        PatchData.NO3 = NO3Amount;

        if (AddSoilCNPatch != null)
            AddSoilCNPatch.Invoke(PatchData);
    }

    /// <summary>Responds to a NUptakesCalculated event</summary>
    /// <param name="SoilNData">NUptakesCalculated data</param>
    [EventHandler]
    public void OnNUptakesCalculated(NUptakesCalculatedType SoilNData)
    {
        // Gets the water uptake for each layer as calculated by an external module (SWIM)
        for (int i_Crop = 0; i_Crop != SoilNData.Uptakes.Length; i_Crop++)
        {
            string MyName = SoilNData.Uptakes[i_Crop].Name;
            if (MyName == thisCropName)
            {
                swardNH4UptakeByAPSIM = new double[dlayer.Length];
                swardNO3UptakeByAPSIM = new double[dlayer.Length];
                for (int layer = 0; layer < SoilNData.Uptakes[i_Crop].NH4Amount.Length; layer++)
                    swardNH4UptakeByAPSIM[layer] = SoilNData.Uptakes[i_Crop].NH4Amount[layer];
                for (int layer = 0; layer < SoilNData.Uptakes[i_Crop].NO3Amount.Length; layer++)
                    swardNO3UptakeByAPSIM[layer] = SoilNData.Uptakes[i_Crop].NO3Amount[layer];
            }
        }
    }

    /// <summary>Sets the soil N stress factor to each species</summary>
    private void SetSpeciesGLFNitrogen()
    {
        //weighted average of species glfN
        if (swardPotGrowthAfterWater > 0.0)
        {
            swardGLFN = 0.0;
            for (int s = 0; s < NumSpecies; s++)
            {
                mySpecies[s].glfN = Math.Min(1.0,
                    Math.Max(0.0, MathUtility.Divide(mySpecies[s].newGrowthN, mySpecies[s].NdemandOpt, 1.0)));
                swardGLFN += mySpecies[s].glfN * MathUtility.Divide(mySpecies[s].dGrowthW, swardPotGrowthAfterWater, 1.0);
            }
        }
        else
        {
            for (int s = 0; s < NumSpecies; s++)
            {
                mySpecies[s].glfN = 1.0;
            }
            swardGLFN = 1.0;
        }
    }

    /// <summary>Val's method for N uptake (not implemented)</summary>
    private void ValsMethod()
    {
        double
            uptake_multiplier = double.MaxValue,
            totSWUptake = soilWaterUptake.Sum();

        double[]
            availableNH4_bylayer = new double[dlayer.Length],
            availableNO3_bylayer = new double[dlayer.Length],
            diffNH4_bylayer = new double[dlayer.Length],
            diffNO3_bylayer = new double[dlayer.Length];

        for (int sLayer = 0; sLayer < dlayer.Length; sLayer++)
        {
            double
                totN = nh4[sLayer] + no3[sLayer],
                fracH2O = soilWaterUptake[sLayer] / totSWUptake;

            if (totN > 0.0)
            {
                availableNH4_bylayer[sLayer] = fracH2O * nh4[sLayer] / totN;
                availableNO3_bylayer[sLayer] = fracH2O * no3[sLayer] / totN;

                //if we have no3 and nh4 in this layer then calculate our uptake multiplier, otherwise set it to 0
                //the idea behind the multiplier is that it allows us to calculate the max amount of N we can extract
                //without forcing any of the layers below 0 AND STILL MAINTAINING THE RATIO as calculated with fracH2O
                //NOTE: it doesn't matter whether we use nh4 or no3 for this calculation, we will get the same answer regardless
                uptake_multiplier = nh4[sLayer] * no3[sLayer] > 0.0
                    ? Math.Min(uptake_multiplier, nh4[sLayer] / availableNH4_bylayer[sLayer])
                    : 0;
            }
            else
            {
                availableNH4_bylayer[sLayer] = 0.0;
                availableNO3_bylayer[sLayer] = 0.0;
            }
        }

        //adjust availability values with the multiplier we just calculated
        availableNH4_bylayer = availableNH4_bylayer.Select(x => x * uptake_multiplier).ToArray();
        availableNO3_bylayer = availableNO3_bylayer.Select(x => x * uptake_multiplier).ToArray();

        //calculate how much no3/nh4 will be left in the soil layers (diff_nxx[layer] = nxx[layer] - availableNH4_bylayer[layer])
        diffNH4_bylayer = nh4.Select((x, sLayer) => Math.Max(0, x - availableNH4_bylayer[sLayer])).ToArray();
        diffNO3_bylayer = no3.Select((x, sLayer) => Math.Max(0, x - availableNO3_bylayer[sLayer])).ToArray();

        //adjust this by the sum of all leftover so we get a ratio we can use later
        double sum_diff = diffNH4_bylayer.Sum() + diffNO3_bylayer.Sum();
        diffNH4_bylayer = diffNH4_bylayer.Select(x => x / sum_diff).ToArray();
        diffNO3_bylayer = diffNO3_bylayer.Select(x => x / sum_diff).ToArray();

        double avail_withwater = availableNH4_bylayer.Sum() + availableNO3_bylayer.Sum();
        double SoilNuptake = soilNH4Uptake.Sum() + soilNO3Uptake.Sum();
        double shortfall_withwater = SoilNuptake - avail_withwater;

        if (shortfall_withwater > 0.0)
        {
            //this cap should not be needed because shortfall is already capped via the math.min in the scaled_demand calcs (leave it here though)
            double scaled_diff = Math.Min(shortfall_withwater / avail_withwater, 1);

            availableNH4_bylayer =
                availableNH4_bylayer.Select((x, sLayer) => x + (shortfall_withwater * diffNH4_bylayer[sLayer]))
                    .ToArray();
            availableNO3_bylayer =
                availableNO3_bylayer.Select((x, sLayer) => x + (shortfall_withwater * diffNO3_bylayer[sLayer]))
                    .ToArray();
        }

        soilNH4Uptake = availableNH4_bylayer.Select(x => x * 1).ToArray();
        soilNO3Uptake = availableNO3_bylayer.Select(x => x * 1).ToArray();

        double[] diffs = soilNO3Uptake.Select((x, i) => Math.Max(no3[i] + x + Epsilon, 0)).ToArray();
        if (diffs.Any(x => x == 0))
            throw new Exception();
    }

    #endregion  - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -

    #region - Organic matter processes  - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -

    /// <summary>Returns detached plant litter to surface organic matter pool</summary>
    /// <param name="amtDM">DM amount</param>
    /// <param name="amtN">N amount</param>
    /// <param name="frac">Fraction=1</param>
    private void DoSurfaceOMReturn(double amtDM, double amtN, double frac)
    {
        if (amtDM < Epsilon)
            return;

        BiomassRemovedType BR = new BiomassRemovedType();
        string[] type = new string[1];
        float[] dltdm = new float[1];
        float[] dltn = new float[1];
        float[] dltp = new float[1];
        float[] fraction = new float[1];

        type[0] = "grass";
        dltdm[0] = (float) amtDM; // kg/ha
        dltn[0] = (float) amtN; // dDM * (float)dead_nconc;
        dltp[0] = dltn[0] * 0.3F; //just a stub here, no P budgeting process in this module
        fraction[0] = (float) frac;

        BR.crop_type = "grass";
        BR.dm_type = type;
        BR.dlt_crop_dm = dltdm;
        BR.dlt_dm_n = dltn;
        BR.dlt_dm_p = dltp;
        BR.fraction_to_residue = fraction;
        BiomassRemoved.Invoke(BR);
    }

    /// <summary>Returns scenesced roots into fresh organic matter pool in soil</summary>
    /// <param name="rootSen">DM amount</param>
    /// <param name="NinRootSen">N amount</param>
    private void DoIncorpFomEvent(double rootSen, double NinRootSen)
    {
        if (rootSen < Epsilon)
            return;

        FOMLayerLayerType[] fomLL = new FOMLayerLayerType[dlayer.Length];

        double dAmtLayer = 0.0; //amount of root litter in a layer
        double dNLayer = 0.0;
        for (int i = 0; i < dlayer.Length; i++)
        {
            dAmtLayer = rootSen * swardRootFraction[i];
            dNLayer = NinRootSen * swardRootFraction[i];

            float amt = (float) dAmtLayer;

            FOMType fom = new FOMType();
            fom.amount = amt;
            fom.N = (float) dNLayer; // 0.03F * amt;    // N in dead root
            fom.C = 0.40F * amt; //40% of OM is C. Actually, 'C' is not used, as shown in DataTypes.xml
            fom.P = 0.0F; //to consider later
            fom.AshAlk = 0.0F; //to consider later

            FOMLayerLayerType Layer = new FOMLayerLayerType();
            Layer.FOM = fom;
            Layer.CNR = 0.0F; //not used
            Layer.LabileP = 0.0F; //not used

            fomLL[i] = Layer;
        }

        FOMLayerType FomLayer = new FOMLayerType();
        FomLayer.Type = thisCropName;
        FomLayer.Layer = fomLL;
        IncorpFOM.Invoke(FomLayer);
    }

    #endregion  - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -

    #endregion  ------------------------------------------------------------------------------------------------------------

    #region Intermittent processes  ----------------------------------------------------------------------------------------

    /// <summary>RespondS to Sow event</summary>
    /// <param name="PSow">Sow data</param>
    [EventHandler]
    public void OnSow(SowType PSow)
    {
        /*SowType is our type and is defined like this:
        <type name="Sow">
        <field name="Cultivar" kind="string" />
        <field name="Population" kind="double" />
        <field name="Depth" kind="double" />
        <field name="MaxCover" kind="double" />
        <field name="BudNumber" kind="double" />
        </type>
        */

        isAlive = true;
        ZeroVars();
        for (int s = 0; s < NumSpecies; s++)
            mySpecies[s].phenoStage = 0;
    }

    /// <summary>Responds to a Kill event</summary>
    /// <param name="KillData">Kill data</param>
    [EventHandler]
    public void OnKillCrop(KillCropType KillData)
    {
        if (KillData.KillFraction < 0.0)
            KillData.KillFraction = 0.0f;

        if (KillData.KillFraction < 1.0)
        {
            for (int s = 0; s < NumSpecies; s++)
            {
                mySpecies[s].KillCrop(KillData.KillFraction);
                mySpecies[s].UpdateAggregatedVariables();
            }

            // Update aggregated variables (whole sward)
            UpdateAggregatedVariables();

            //Let user know (via summary) that a fraction of plants was killed
            Console.WriteLine(myClock.Today.ToString("dd MMMM yyyy") + " (Day of year=" +
                              myClock.Today.DayOfYear + "), " + thisCropName + ":");
            Console.WriteLine("     Pasture is being partially killed, " +
                              (KillData.KillFraction * 100.0).ToString("#0.0") + "% of live biomass in now dead");
        }
        else
        {
            // the pasture will be ended
            OnEndCrop();
        }
    }

    /// <summary>Responds to a EndCrop event</summary>
    [EventHandler]
    public void OnEndCrop()
    {
        //Above_ground part returns to surface OM completely (frac = 1.0)
        DoSurfaceOMReturn(AboveGroundWt, AboveGroundN, 1.0);

        //Incorporate root mass in soil fresh organic matter
        DoIncorpFomEvent(BelowGroundWt, BelowGroundN);


        // Zero variables and update the aggregated variables (LAI, height, etc.)
        for (int s = 0; s < NumSpecies; s++)
        {
            ZeroVars();
            mySpecies[s].leaves.DoResetOrgan();
            mySpecies[s].stems.DoResetOrgan();
            mySpecies[s].stolons.DoResetOrgan();
            mySpecies[s].roots.DoResetOrgan();

            mySpecies[s].UpdateAggregatedVariables();
        }

        // Update aggregated variables (whole sward)
        UpdateAggregatedVariables();

        isAlive = false;

        //Let user know (via summary) that the pasture has end
        Console.WriteLine(myClock.Today.ToString("dd MMMM yyyy") + " (Day of year=" + myClock.Today.DayOfYear + "), " + thisCropName + ":");
        Console.WriteLine("     EndCrop - Pasture is now dead");
    }

    /// <summary>Zero out some variables</summary>
    private void ZeroVars()
    {
        //root depth
        swardRootDepth = 0.0;

        //daily changes
        swardNetPotentialGrowth = swardPotGrowthAfterWater = swardPotGrowthAfterNutrient = 0.0;
        swardLitterDM = swardLitterN = 0.0;
        swardSenescedRootDM = swardSenescedRootN = 0.0;

        swardWaterDemand = 0.0;
        swardSoilNDemand = 0.0;

        //species and organs
        for (int s = 0; s < NumSpecies; s++)
            mySpecies[s].RefreshVariables();
    }

    /// <summary>Respond to a Reset event</summary>
    [EventHandler]
    public void OnReset()
    {

        // set all species to their initial state (DM, N, LAI, etc.)
        for (int s = 0; s < NumSpecies; s++)
        {
            if (!usingSpeciesRoot)
            {
                // only sward is considered, use root system of species1
                mySpecies[s].InitialState.RootDepth = mySpecies[0].rootDepth;
            }

            // set initial state
            SetSpeciesState(s, mySpecies[s].InitialState);

            // get the deepest root as sward depth
            if (mySpecies[s].rootDepth > swardRootDepth)
            {
                swardRootDepth = mySpecies[s].rootDepth;
                swardRootZoneBottomLayer = mySpecies[s].layerBottomRootZone;
            }
        }

        // check root distribution
        int nLayers = dlayer.Length;
        swardTargetRootAllocation = new double[nLayers];
        swardRootFraction = new double[nLayers];
        if (!usingSpeciesRoot)
        {
            // only sward height is considered, use root system of species 1
            for (int layer = 0; layer < nLayers; layer++)
            {
                swardTargetRootAllocation[layer] = mySpecies[0].targetRootAllocation[layer];
                swardRootFraction[layer] = mySpecies[0].rootFraction[layer];
            }

            for (int s = 0; s < NumSpecies; s++)
            {
                mySpecies[s].rootFraction = new double[nLayers];
                for (int layer = 0; layer < nLayers; layer++)
                {
                    mySpecies[s].targetRootAllocation[layer] = swardTargetRootAllocation[layer];
                    mySpecies[s].rootFraction[layer] = swardRootFraction[layer];
                }
            }
        }

        // update aggregated variables (whole sward)
        UpdateAggregatedVariables();
    }

    /// <summary>Allow setting up the DM and N amounts of any species</summary>
    /// <param name="NewSetState">New set of values for DM and N of given species</param>
    /// <remarks>
    /// The type NewSetState contains:
    ///   - speciesID: List with the index of species to be changed
    ///   - dmShoot: Array of DM shoot values for each species being changed
    ///   - dmRoot: Array of DM root values for each species being changed
    ///   - rootDepth: Array of root depth values for each species being changed
    ///   - dmFractions: Array with values of DM fractions for each pool in each species being changed
    ///   - nConcentrations: Array of N concentration values for each pool in each species being changed
    /// </remarks>
    [EventHandler]
    public void OnSetSpeciesState(SetSpeciesStateType NewSetState)
    {
        // in theory all parameters are optional, but dmFractions and nConcentrations need to be initialised or APSIM crashes


        foreach (int sp in NewSetState.speciesID)
        {
            //initialise state variable
            SpeciesBasicState NewState = new SpeciesBasicState();

            // Check DM shoot
            if (NewSetState.dmShoot.Length > 0)
            {
                //check for negative value
                if (NewSetState.dmShoot[sp] < -Epsilon)
                    throw new Exception("Attempt to set shoot DM of " + mySpecies[sp].speciesName + " to a negative value - OnSetSpeciesState");

                // New DM being set, check DM fractions
                if (NewSetState.dmFractions.Length > 0)
                {
                    // New fractions given
                    NewState.DMWeight[0] = NewSetState.dmShoot[sp] * NewSetState.dmFractions[sp].Leaf1;
                    NewState.DMWeight[1] = NewSetState.dmShoot[sp] * NewSetState.dmFractions[sp].Leaf2;
                    NewState.DMWeight[2] = NewSetState.dmShoot[sp] * NewSetState.dmFractions[sp].Leaf3;
                    NewState.DMWeight[3] = NewSetState.dmShoot[sp] * NewSetState.dmFractions[sp].Leaf4;
                    NewState.DMWeight[4] = NewSetState.dmShoot[sp] * NewSetState.dmFractions[sp].Stem1;
                    NewState.DMWeight[5] = NewSetState.dmShoot[sp] * NewSetState.dmFractions[sp].Stem2;
                    NewState.DMWeight[6] = NewSetState.dmShoot[sp] * NewSetState.dmFractions[sp].Stem3;
                    NewState.DMWeight[7] = NewSetState.dmShoot[sp] * NewSetState.dmFractions[sp].Stem4;
                    NewState.DMWeight[8] = NewSetState.dmShoot[sp] * NewSetState.dmFractions[sp].Stolon1;
                    NewState.DMWeight[9] = NewSetState.dmShoot[sp] * NewSetState.dmFractions[sp].Stolon2;
                    NewState.DMWeight[10] = NewSetState.dmShoot[sp] * NewSetState.dmFractions[sp].Stolon3;

                    // check for mass balance
                    if (NewState.DMWeight.Sum() - NewSetState.dmShoot[sp] < -Epsilon)
                        throw new Exception("Fractions to partition shoot DM of " + mySpecies[sp].speciesName
                                            + " did not add to one - OnSetSpeciesState");
                }
                else
                {
                    if (mySpecies[sp].AboveGroundWt > 0.0)
                    {
                        // Use current fractions
                        NewState.DMWeight[0] = NewSetState.dmShoot[sp] * mySpecies[sp].leaves.tissue[0].DM / mySpecies[sp].AboveGroundWt;
                        NewState.DMWeight[1] = NewSetState.dmShoot[sp] * mySpecies[sp].leaves.tissue[1].DM / mySpecies[sp].AboveGroundWt;
                        NewState.DMWeight[2] = NewSetState.dmShoot[sp] * mySpecies[sp].leaves.tissue[2].DM / mySpecies[sp].AboveGroundWt;
                        NewState.DMWeight[3] = NewSetState.dmShoot[sp] * mySpecies[sp].leaves.tissue[3].DM / mySpecies[sp].AboveGroundWt;
                        NewState.DMWeight[4] = NewSetState.dmShoot[sp] * mySpecies[sp].stems.tissue[0].DM / mySpecies[sp].AboveGroundWt;
                        NewState.DMWeight[5] = NewSetState.dmShoot[sp] * mySpecies[sp].stems.tissue[1].DM / mySpecies[sp].AboveGroundWt;
                        NewState.DMWeight[6] = NewSetState.dmShoot[sp] * mySpecies[sp].stems.tissue[2].DM / mySpecies[sp].AboveGroundWt;
                        NewState.DMWeight[7] = NewSetState.dmShoot[sp] * mySpecies[sp].stems.tissue[3].DM / mySpecies[sp].AboveGroundWt;
                        NewState.DMWeight[8] = NewSetState.dmShoot[sp] * mySpecies[sp].stolons.tissue[0].DM / mySpecies[sp].AboveGroundWt;
                        NewState.DMWeight[9] = NewSetState.dmShoot[sp] * mySpecies[sp].stolons.tissue[1].DM / mySpecies[sp].AboveGroundWt;
                        NewState.DMWeight[10] = NewSetState.dmShoot[sp] * mySpecies[sp].stolons.tissue[2].DM / mySpecies[sp].AboveGroundWt;
                    }
                    else
                    {
                        // Use the default initial fractions
                        double defaultDM = mySpecies[sp].InitialState.DMWeight.Sum();
                        NewState.DMWeight[0] = NewSetState.dmShoot[sp] * mySpecies[sp].InitialState.DMWeight[0] / defaultDM;
                        NewState.DMWeight[1] = NewSetState.dmShoot[sp] * mySpecies[sp].InitialState.DMWeight[1] / defaultDM;
                        NewState.DMWeight[2] = NewSetState.dmShoot[sp] * mySpecies[sp].InitialState.DMWeight[2] / defaultDM;
                        NewState.DMWeight[3] = NewSetState.dmShoot[sp] * mySpecies[sp].InitialState.DMWeight[3] / defaultDM;
                        NewState.DMWeight[4] = NewSetState.dmShoot[sp] * mySpecies[sp].InitialState.DMWeight[4] / defaultDM;
                        NewState.DMWeight[5] = NewSetState.dmShoot[sp] * mySpecies[sp].InitialState.DMWeight[5] / defaultDM;
                        NewState.DMWeight[6] = NewSetState.dmShoot[sp] * mySpecies[sp].InitialState.DMWeight[6] / defaultDM;
                        NewState.DMWeight[7] = NewSetState.dmShoot[sp] * mySpecies[sp].InitialState.DMWeight[7] / defaultDM;
                        NewState.DMWeight[8] = NewSetState.dmShoot[sp] * mySpecies[sp].InitialState.DMWeight[8] / defaultDM;
                        NewState.DMWeight[9] = NewSetState.dmShoot[sp] * mySpecies[sp].InitialState.DMWeight[9] / defaultDM;
                        NewState.DMWeight[10] = NewSetState.dmShoot[sp] * mySpecies[sp].InitialState.DMWeight[10] / defaultDM;
                    }
                }
            }
            else
            {
                // Keep current DM
                NewState.DMWeight[0] = mySpecies[sp].leaves.tissue[0].DM;
                NewState.DMWeight[1] = mySpecies[sp].leaves.tissue[1].DM;
                NewState.DMWeight[2] = mySpecies[sp].leaves.tissue[2].DM;
                NewState.DMWeight[3] = mySpecies[sp].leaves.tissue[3].DM;
                NewState.DMWeight[4] = mySpecies[sp].stems.tissue[0].DM;
                NewState.DMWeight[5] = mySpecies[sp].stems.tissue[1].DM;
                NewState.DMWeight[6] = mySpecies[sp].stems.tissue[2].DM;
                NewState.DMWeight[7] = mySpecies[sp].stems.tissue[3].DM;
                NewState.DMWeight[8] = mySpecies[sp].stolons.tissue[0].DM;
                NewState.DMWeight[9] = mySpecies[sp].stolons.tissue[1].DM;
                NewState.DMWeight[10] = mySpecies[sp].stolons.tissue[2].DM;
            }

            // Check DM root
            if (NewSetState.dmRoot.Length > 0)
            {
                if (NewSetState.dmRoot[sp] >= 0.0)
                    NewState.DMWeight[11] = NewSetState.dmRoot[sp];
                else
                    throw new Exception("Attempt to set root DM of " + mySpecies[sp].speciesName + " to a negative value - OnSetSpeciesState");
            }
            else
                NewState.DMWeight[11] = mySpecies[sp].roots.DMGreen;

            // Check root depth
            if (NewSetState.rootDepth.Length > 0)
            {
                if (NewSetState.rootDepth[sp] >= 0.0)
                    NewState.RootDepth = NewSetState.rootDepth[sp];
                else
                    throw new Exception("Attempt to set root depth of " + mySpecies[sp].speciesName + " to a negative value - OnSetSpeciesState");
            }
            else
                NewState.RootDepth = mySpecies[sp].rootDepth;

            // Check N concentrations
            if (NewSetState.nConcentrations.Length > 0)
            {
                // new values given
                if (NewSetState.nConcentrations[sp].Leaf1 >= 0.0)
                    NewState.NAmount[0] = NewState.DMWeight[0] * NewSetState.nConcentrations[sp].Leaf1;
                else
                    throw new Exception("Attempt to set leaf N concentration of " + mySpecies[sp].speciesName +
                                        " to a negative value - OnSetSpeciesState");

                if (NewSetState.nConcentrations[sp].Leaf2 >= 0.0)
                    NewState.NAmount[1] = NewState.DMWeight[1] * NewSetState.nConcentrations[sp].Leaf2;
                else
                    throw new Exception("Attempt to set leaf N concentration of " + mySpecies[sp].speciesName +
                                        " to a negative value - OnSetSpeciesState");

                if (NewSetState.nConcentrations[sp].Leaf3 >= 0.0)
                    NewState.NAmount[2] = NewState.DMWeight[2] * NewSetState.nConcentrations[sp].Leaf3;
                else
                    throw new Exception("Attempt to set leaf N concentration of " + mySpecies[sp].speciesName +
                                        " to a negative value - OnSetSpeciesState");

                if (NewSetState.nConcentrations[sp].Leaf4 >= 0.0)
                    NewState.NAmount[3] = NewState.DMWeight[3] * NewSetState.nConcentrations[sp].Leaf4;
                else
                    throw new Exception("Attempt to set leaf N concentration of " + mySpecies[sp].speciesName +
                                        " to a negative value - OnSetSpeciesState");

                if (NewSetState.nConcentrations[sp].Stem1 >= 0.0)
                    NewState.NAmount[4] = NewState.DMWeight[4] * NewSetState.nConcentrations[sp].Stem1;
                else
                    throw new Exception("Attempt to set stem N concentration of " + mySpecies[sp].speciesName +
                                        " to a negative value - OnSetSpeciesState");

                if (NewSetState.nConcentrations[sp].Stem2 >= 0.0)
                    NewState.NAmount[5] = NewState.DMWeight[5] * NewSetState.nConcentrations[sp].Stem2;
                else
                    throw new Exception("Attempt to set stem N concentration of " + mySpecies[sp].speciesName +
                                        " to a negative value - OnSetSpeciesState");

                if (NewSetState.nConcentrations[sp].Stem3 >= 0.0)
                    NewState.NAmount[6] = NewState.DMWeight[6] * NewSetState.nConcentrations[sp].Stem3;
                else
                    throw new Exception("Attempt to set stem N concentration of " + mySpecies[sp].speciesName +
                                        " to a negative value - OnSetSpeciesState");

                if (NewSetState.nConcentrations[sp].Stem4 >= 0.0)
                    NewState.NAmount[7] = NewState.DMWeight[7] * NewSetState.nConcentrations[sp].Stem4;
                else
                    throw new Exception("Attempt to set stem N concentration of " + mySpecies[sp].speciesName +
                                        " to a negative value - OnSetSpeciesState");

                if (NewSetState.nConcentrations[sp].Stolon1 >= 0.0)
                    NewState.NAmount[8] = NewState.DMWeight[8] * NewSetState.nConcentrations[sp].Stolon1;
                else
                    throw new Exception("Attempt to set stolon N concentration of " + mySpecies[sp].speciesName +
                                        " to a negative value - OnSetSpeciesState");

                if (NewSetState.nConcentrations[sp].Stolon2 >= 0.0)
                    NewState.NAmount[9] = NewState.DMWeight[9] * NewSetState.nConcentrations[sp].Stolon2;
                else
                    throw new Exception("Attempt to set stolon N concentration of " + mySpecies[sp].speciesName +
                                        " to a negative value - OnSetSpeciesState");

                if (NewSetState.nConcentrations[sp].Stolon3 >= 0.0)
                    NewState.NAmount[10] = NewState.DMWeight[10] * NewSetState.nConcentrations[sp].Stolon3;
                else
                    throw new Exception("Attempt to set stolon N concentration of " + mySpecies[sp].speciesName +
                                        " to a negative value - OnSetSpeciesState");

                if (NewSetState.nConcentrations[sp].Roots >= 0.0)
                    NewState.NAmount[11] = NewState.DMWeight[11] * NewSetState.nConcentrations[sp].Roots;
                else
                    throw new Exception("Attempt to set root N concentration of " + mySpecies[sp].speciesName +
                                        " to a negative value - OnSetSpeciesState");
            }
            else
            {
                // Use current values
                NewState.NAmount[0] = NewState.DMWeight[0] * mySpecies[sp].leaves.tissue[0].Nconc;
                NewState.NAmount[1] = NewState.DMWeight[1] * mySpecies[sp].leaves.tissue[1].Nconc;
                NewState.NAmount[2] = NewState.DMWeight[2] * mySpecies[sp].leaves.tissue[2].Nconc;
                NewState.NAmount[3] = NewState.DMWeight[3] * mySpecies[sp].leaves.tissue[3].Nconc;
                NewState.NAmount[4] = NewState.DMWeight[4] * mySpecies[sp].stems.tissue[0].Nconc;
                NewState.NAmount[5] = NewState.DMWeight[5] * mySpecies[sp].stems.tissue[1].Nconc;
                NewState.NAmount[6] = NewState.DMWeight[6] * mySpecies[sp].stems.tissue[2].Nconc;
                NewState.NAmount[7] = NewState.DMWeight[7] * mySpecies[sp].stems.tissue[3].Nconc;
                NewState.NAmount[8] = NewState.DMWeight[8] * mySpecies[sp].stolons.tissue[0].Nconc;
                NewState.NAmount[9] = NewState.DMWeight[9] * mySpecies[sp].stolons.tissue[1].Nconc;
                NewState.NAmount[10] = NewState.DMWeight[10] * mySpecies[sp].stolons.tissue[2].Nconc;
                NewState.NAmount[11] = NewState.DMWeight[11] * mySpecies[sp].roots.tissue[0].Nconc;
            }

            // Set the species
            SetSpeciesState(sp, NewState);
        }

        // Update aggregated variables (whole sward)
        UpdateAggregatedVariables();
    }

    /// <summary>Harvest (remove) an amount of plants</summary>
    /// <param name="type">amount type</param>
    /// <param name="amount">DM amount</param>
    public void Harvest(string type, double amount)
    {
        GrazeType GZ = new GrazeType();
        GZ.amount = (float) amount;
        GZ.type = type;
        OnGraze(GZ);
    }

    /// <summary>Respond to a Graze event</summary>
    /// <param name="grazeData">Graze</param>
    [EventHandler]
    public void OnGraze(GrazeType grazeData)
    {
        double amountAvailable = HarvestableWt;
        if (isAlive && (amountAvailable > Epsilon))
        {
            // zero the sward variables
            swardHarvestedDM = 0.0;
            swardHarvestedN = 0.0;
            swardHarvestDigestibility = 0.0;

            // get the amount required to remove
            double AmountRequired = 0.0;
            if (grazeData.type.ToLower() == "SetResidueAmount".ToLower())
            {
                // Remove all DM above given residual amount
                AmountRequired = Math.Max(0.0, StandingHerbageWt - grazeData.amount);
            }
            else if (grazeData.type.ToLower() == "SetRemoveAmount".ToLower())
            {
                // Attempt to remove a given amount
                AmountRequired = Math.Max(0.0, grazeData.amount);
            }
            else
            {
                Console.WriteLine("Method to set amount to remove was not recognized, command will be ignored");
            }

            // get the actual amount to be removed
            double AmountToRemove = Math.Min(AmountRequired, amountAvailable);

            if (AmountToRemove > 0.0)
            {
                // get the actual amounts being removed for each species
                for (int s = 0; s < NumSpecies; s++)
                {
                    // get the fraction to required for each mySpecies, partition according to available DM to harvest
                    FractionToHarvest[s] = mySpecies[s].HarvestableWt / amountAvailable;
                    swardHarvestedDM += mySpecies[s].RemoveDM(AmountToRemove * FractionToHarvest[s]);
                    swardHarvestedN += mySpecies[s].Ndefoliated;

                    // get digestibility of harvested material
                    swardHarvestDigestibility += mySpecies[s].digestDefoliated * mySpecies[s].dmdefoliated / AmountToRemove;
                }

                // check some variables
                swardHarvestDigestibility = Math.Min(1.0, swardHarvestDigestibility);
                if (Math.Abs(swardHarvestedDM - AmountToRemove) > Epsilon)
                    throw new Exception("OnGraze - removal of DM resulted in loss of mass balance");

                // Update aggregated variables (whole sward)
                UpdateAggregatedVariables();
            }
        }
    }

    /// <summary>Respond to a RemoveCropBiomass event</summary>
    /// <param name="removeData">RemoveCropBiomass</param>
    /// <remarks>
    /// It is responsibility of the calling module to check the amount of herbage
    /// in each pools of AboveGroundWt and set the correct amount in 'removeData'.
    /// No checking here for whether the removing amount passed are too high...
    /// </remarks>
    [EventHandler]
    public void Onremove_crop_biomass(RemoveCropBiomassType removeData)
    {
        const double gm2ha = 10; // constant for conversion of g/m^2 to kg/ha,
        // removeData.dm.dlt is passed here in g/m^2

        double existingDMtotal;
        double existingDMspecies;
        double dmToRemove;
        double nToRemove;
        double removeFraction;
        double amountRequested = 0.0;

        for (int i = 0; i < removeData.dm.Length; i++) //for each major pool (green or dead)
        {
            for (int j = 0; j < removeData.dm[i].dlt.Length; j++) //for each plant part (leaves, stems, etc)
            {
                if (removeData.dm[i].pool == "green" && removeData.dm[i].part[j] == "leaf")
                {
                    existingDMtotal = LeafLiveWt;
                    if (existingDMtotal > 0.0) //responsibility of other modules to check the amount
                    {
                        for (int s = 0; s < NumSpecies; s++) //for each species
                        {
                            existingDMspecies = mySpecies[s].leaves.DMGreen;
                            dmToRemove = gm2ha * removeData.dm[i].dlt[j] * (existingDMspecies / existingDMtotal);
                            amountRequested += dmToRemove;
                            nToRemove = dmToRemove * mySpecies[s].leaves.NconcGreen;
                            removeFraction = MathUtility.Divide(mySpecies[s].leaves.tissue[0].DM, existingDMspecies, 0.0);
                            mySpecies[s].leaves.tissue[0].DM -= dmToRemove * removeFraction;
                            mySpecies[s].leaves.tissue[0].Namount -= nToRemove * removeFraction;
                            mySpecies[s].dmdefoliated += dmToRemove * removeFraction;
                            mySpecies[s].Ndefoliated += nToRemove * removeFraction;
                            removeFraction = MathUtility.Divide(mySpecies[s].leaves.tissue[1].DM, existingDMspecies, 0.0);
                            mySpecies[s].leaves.tissue[1].DM -= dmToRemove * removeFraction;
                            mySpecies[s].leaves.tissue[1].Namount -= nToRemove * removeFraction;
                            mySpecies[s].dmdefoliated += dmToRemove * removeFraction;
                            mySpecies[s].Ndefoliated += nToRemove * removeFraction;
                            removeFraction = MathUtility.Divide(mySpecies[s].leaves.tissue[2].DM, existingDMspecies, 0.0);
                            mySpecies[s].leaves.tissue[2].DM -= dmToRemove * removeFraction;
                            mySpecies[s].leaves.tissue[2].Namount -= nToRemove * removeFraction;
                            mySpecies[s].dmdefoliated += dmToRemove * removeFraction;
                            mySpecies[s].Ndefoliated += nToRemove * removeFraction;
                        }
                    }
                }
                else if (removeData.dm[i].pool == "green" && removeData.dm[i].part[j] == "stem")
                {
                    existingDMtotal = StemLiveWt;
                    if (existingDMtotal > 0) //responsibility of other modules to check the amount
                    {
                        for (int s = 0; s < NumSpecies; s++)
                        {
                            existingDMspecies = mySpecies[s].stems.DMGreen;
                            dmToRemove = gm2ha * removeData.dm[i].dlt[j] * (existingDMspecies / existingDMtotal);
                            amountRequested += dmToRemove;
                            nToRemove = dmToRemove * mySpecies[s].stems.NconcGreen;
                            removeFraction = MathUtility.Divide(mySpecies[s].stems.tissue[0].DM, existingDMspecies, 0.0);
                            mySpecies[s].stems.tissue[0].DM -= dmToRemove * removeFraction;
                            mySpecies[s].stems.tissue[0].Namount -= nToRemove * removeFraction;
                            mySpecies[s].dmdefoliated += dmToRemove * removeFraction;
                            mySpecies[s].Ndefoliated += nToRemove * removeFraction;
                            removeFraction = MathUtility.Divide(mySpecies[s].stems.tissue[1].DM, existingDMspecies, 0.0);
                            mySpecies[s].stems.tissue[1].DM -= dmToRemove * removeFraction;
                            mySpecies[s].stems.tissue[1].Namount -= nToRemove * removeFraction;
                            mySpecies[s].dmdefoliated += dmToRemove * removeFraction;
                            mySpecies[s].Ndefoliated += nToRemove * removeFraction;
                            removeFraction = MathUtility.Divide(mySpecies[s].stems.tissue[2].DM, existingDMspecies, 0.0);
                            mySpecies[s].stems.tissue[2].DM -= dmToRemove * removeFraction;
                            mySpecies[s].stems.tissue[2].Namount -= nToRemove * removeFraction;
                            mySpecies[s].dmdefoliated += dmToRemove * removeFraction;
                            mySpecies[s].Ndefoliated += nToRemove * removeFraction;
                        }
                    }
                }
                else if (removeData.dm[i].pool == "dead" && removeData.dm[i].part[j] == "leaf")
                {
                    existingDMtotal = LeafDeadWt;
                    if (existingDMtotal > 0.0) //responsibility of other modules to check the amount
                    {
                        for (int s = 0; s < NumSpecies; s++)
                        {
                            existingDMspecies = mySpecies[s].leaves.tissue[3].DM;
                            dmToRemove = gm2ha * removeData.dm[i].dlt[j] * (existingDMspecies / existingDMtotal);
                            amountRequested += dmToRemove;
                            nToRemove = dmToRemove * mySpecies[s].leaves.tissue[3].Nconc;
                            mySpecies[s].leaves.tissue[3].DM -= dmToRemove;
                            mySpecies[s].leaves.tissue[3].Namount -= nToRemove;
                            mySpecies[s].dmdefoliated += dmToRemove;
                            mySpecies[s].Ndefoliated += nToRemove;
                        }
                    }
                }
                else if (removeData.dm[i].pool == "dead" && removeData.dm[i].part[j] == "stem")
                {
                    existingDMtotal = StemDeadWt;
                    if (existingDMtotal > 0.0) //responsibility of other modules to check the amount
                    {
                        for (int s = 0; s < NumSpecies; s++)
                        {
                            existingDMspecies = mySpecies[s].stems.tissue[3].DM;
                            dmToRemove = gm2ha * removeData.dm[i].dlt[j] * (existingDMspecies / existingDMtotal);
                            amountRequested += dmToRemove;
                            nToRemove = dmToRemove * mySpecies[s].stems.tissue[3].Nconc;
                            mySpecies[s].stems.tissue[3].DM -= dmToRemove;
                            mySpecies[s].stems.tissue[3].Namount -= nToRemove;
                            mySpecies[s].dmdefoliated += dmToRemove;
                            mySpecies[s].Ndefoliated += nToRemove;
                        }
                    }
                }
            }
        }

        swardHarvestedDM = 0.0;
        swardHarvestedN = 0.0;
        for (int s = 0; s < NumSpecies; s++)
        {
            swardHarvestedDM += mySpecies[s].dmdefoliated;
            swardHarvestedN += mySpecies[s].Ndefoliated;

            // Update the aggregated variables (LAI, height, etc.)
            mySpecies[s].UpdateAggregatedVariables();
        }

        // check mass balance
        if (Math.Abs(swardHarvestedDM - amountRequested) > Epsilon)
            throw new Exception("OnRemove - removal of DM resulted in loss of mass balance");

        // Update aggregated variables (whole sward)
        UpdateAggregatedVariables();

        //In this routine of no selection among species, the removed tissue from different species
        //will be in proportion with existing mass of each species.
        //The digestibility below is an approximation (= that of pasture swards).
        //It is more reasonable to calculate it organ-by-organ for each species, then put them together.
        swardHarvestDigestibility = HerbageDigestibility;
    }

    #endregion  ------------------------------------------------------------------------------------------------------------

    #region Auxiliary functions and processes  -----------------------------------------------------------------------------

    /// <summary>
    /// The following helper functions [VDP and svp] are for calculating Fvdp
    /// </summary>
    /// <returns></returns>
    private double VPD()
    {
        double SVPfrac = 0.66F;
        double VPDmint = svp(MetData.MinT) - MetData.vp;
        VPDmint = Math.Max(VPDmint, 0.0);

        double VPDmaxt = svp(MetData.MaxT) - MetData.vp;
        VPDmaxt = Math.Max(VPDmaxt, 0.0);

        double vdp = (SVPfrac * VPDmaxt)
                     + ((1 - SVPfrac) * VPDmint);
        return vdp;
    }

    private double svp(double temp) // from Growth.for documented in MicroMet
    {
        return 6.1078 * Math.Exp(17.269 * temp / (237.3 + temp));
    }

    #endregion  ------------------------------------------------------------------------------------------------------------

    #region Model outputs  -------------------------------------------------------------------------------------------------

    #region - Sward outputs - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -

    ////- General properties >>>  - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -

    /// <summary>Generic type of crop</summary>
    [Output] //  useful for SWIM
    [Description("Generic type of crop")]
    [Units("")]
    public string Crop_type
    {
        get { return thisCropName; }
    }

    /// <summary>Name of this crop</summary>
    [Output]
    [Description("Name of this crop")]
    [Units("")]
    public string Crop_name
    {
        get { return thisCropName; }
    }

    private bool isAlive = true;

    /// <summary>Flag signalling whether plants are alive</summary>
    [Output]
    [Description("Flag signalling whether plants are alive")]
    [Units("true/false")]
    public bool IsAlive
    {
        get { return isAlive; }
    }

    /// <summary>Plant status (dead, alive, etc.)</summary>
    [Output]
    [Description("Plant status (dead, alive, etc.)")]
    [Units("")]
    public string plant_status
    {
        get
        {
            if (isAlive) return "alive";
            else return "out";
        }
    }

    /// <summary>Plant development stage number</summary>
    [Output]
    [Description("Plant development stage number")]
    [Units("")]
    public int Stage
    {
        //An approximate of the stages corresponding to that of other arable crops for management application settings.
        //Phenological stage of the first species (ryegrass) is used for this approximation
        get
        {
            int cropStage = 0; //default as "phase out"
            if (isAlive)
            {
                if (mySpecies[0].phenoStage == 0)
                    cropStage = 1; //"sowing & germination";
                if (mySpecies[0].phenoStage == 1)
                    cropStage = 3; //"emergence";
            }
            return cropStage;
        }
    }

    ////- DM and C outputs >>>  - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
    /// <summary>Total amount of C in the plant</summary>
    [Output]
    [Description("Total amount of C in the plant")]
    [Units("kgC/ha")]
    public double TotalC
    {
        get { return TotalWt * CarbonFractionDM; }
    }

    /// <summary>Total dry matter weight of plant</summary>
    [Output]
    [Description("Total dry matter weight of plant")]
    [Units("kgDM/ha")]
    public double TotalWt
    {
        get { return AboveGroundWt + BelowGroundWt; }
    }

    /// <summary>Dry matter weight of plant above ground</summary>
    [Output]
    [Description("Dry matter weight of the plant above ground")]
    [Units("kgDM/ha")]
    public double AboveGroundWt
    {
        get
        {
            double result = 0.0;
            for (int s = 0; s < NumSpecies; s++)
                result += mySpecies[s].AboveGroundWt;
            return result;
        }
    }

    /// <summary>Dry matter weight of live tissues above ground</summary>
    [Output]
    [Description("Dry matter weight of live tissues above ground")]
    [Units("kgDM/ha")]
    public double AboveGroundLiveWt
    {
        get
        {
            double result = 0.0;
            for (int s = 0; s < NumSpecies; s++)
                result += mySpecies[s].AboveGroundLiveWt;
            return result;
        }
    }

    /// <summary>Dry matter weight of dead tissues above ground</summary>
    [Output]
    [Description("Dry matter weight of dead tissues above ground")]
    [Units("kgDM/ha")]
    public double AboveGroundDeadWt
    {
        get
        {
            double result = 0.0;
            for (int s = 0; s < NumSpecies; s++)
                result += mySpecies[s].AboveGroundDeadWt;
            return result;
        }
    }

    /// <summary>Dry matter weight of plant below ground</summary>
    [Output]
    [Description("Dry matter weight of the plant below ground")]
    [Units("kgDM/ha")]
    public double BelowGroundWt
    {
        get
        {
            double result = 0.0;
            for (int s = 0; s < NumSpecies; s++)
                result += mySpecies[s].roots.DMTotal;
            return result;
        }
    }

    /// <summary>Dry matter weight of standing herbage</summary>
    [Output]
    [Description("Dry matter weight of standing herbage")]
    [Units("kgDM/ha")]
    public double StandingHerbageWt
    {
        get
        {
            double result = 0.0;
            for (int s = 0; s < NumSpecies; s++)
                result += mySpecies[s].StandingWt;
            return result;
        }
    }

    /// <summary>Dry matter weight of plant's leaves</summary>
    [Output]
    [Description("Dry matter weight of plant's leaves")]
    [Units("kgDM/ha")]
    public double LeafWt
    {
        get
        {
            double result = 0.0;
            for (int s = 0; s < NumSpecies; s++)
                result += mySpecies[s].leaves.DMTotal;
            return result;
        }
    }

    /// <summary>Dry matter weight of live leaves</summary>
    [Output]
    [Description("Dry matter weight of live leaves")]
    [Units("kgDM/ha")]
    public double LeafLiveWt
    {
        get
        {
            double result = 0.0;
            for (int s = 0; s < NumSpecies; s++)
                result += mySpecies[s].leaves.DMGreen;
            return result;
        }
    }

    /// <summary>Dry matter weight of dead leaves</summary>
    [Output]
    [Description("Dry matter weight of dead leaves")]
    [Units("kgDM/ha")]
    public double LeafDeadWt
    {
        get
        {
            double result = 0.0;
            for (int s = 0; s < NumSpecies; s++)
                result += mySpecies[s].leaves.DMDead;
            return result;
        }
    }

    /// <summary>Dry matter weight of plant's stems and sheath</summary>
    [Output]
    [Description("Dry matter weight of plant's stems and sheath")]
    [Units("kgDM/ha")]
    public double StemWt
    {
        get
        {
            double result = 0.0;
            for (int s = 0; s < NumSpecies; s++)
                result += mySpecies[s].stems.DMTotal;
            return result;
        }
    }

    /// <summary>Dry matter weight of alive stems and sheath</summary>
    [Output]
    [Description("Dry matter weight of alive stems and sheath")]
    [Units("kgDM/ha")]
    public double StemLiveWt
    {
        get
        {
            double result = 0.0;
            for (int s = 0; s < NumSpecies; s++)
                result += mySpecies[s].stems.DMGreen;
            return result;
        }
    }

    /// <summary>Dry matter weight of dead stems and sheath</summary>
    [Output]
    [Description("Dry matter weight of dead stems and sheath")]
    [Units("kgDM/ha")]
    public double StemDeadWt
    {
        get
        {
            double result = 0.0;
            for (int s = 0; s < NumSpecies; s++)
                result += mySpecies[s].stems.DMDead;
            return result;
        }
    }

    /// <summary>Dry matter weight of plant's stolons</summary>
    [Output]
    [Description("Dry matter weight of plant's stolons")]
    [Units("kgDM/ha")]
    public double StolonWt
    {
        get
        {
            double result = 0.0;
            for (int s = 0; s < NumSpecies; s++)
                result += mySpecies[s].stolons.DMGreen;
            return result;
        }
    }

    /// <summary>Dry matter weight of plant's roots</summary>
    [Output]
    [Description("Dry matter weight of plant's roots")]
    [Units("kgDM/ha")]
    public double RootWt
    {
        get
        {
            double result = 0.0;
            for (int s = 0; s < NumSpecies; s++)
                result += mySpecies[s].roots.DMGreen;
            return result;
        }
    }

    //for consistency (backward compatibility), publish some variables similar with other plant modules (useful Onremove_crop_biomass())
    /// <summary>Dry matter weight of plant above ground</summary>
    [Output]
    [Description("Dry matter weight of plant above ground")]
    [Units("kg/ha")]
    public double biomass
    {
        get { return AboveGroundWt; }
    }

    /// <summary>Dry matter weight of live leaves</summary>
    [Output]
    [Description("Dry matter weight of live leaves")]
    [Units("g/m^2")]
    public double leafgreenwt
    {
        get { return LeafLiveWt * 0.10; }
    }

    /// <summary>Dry matter weight of dead leaves</summary>
    [Output]
    [Description("Dry matter weight of dead leaves")]
    [Units("g/m^2")]
    public double stemgreenwt
    {
        get { return StemLiveWt * 0.10; }
    }

    /// <summary>Dry matter weight of dead stems and sheath</summary>
    [Output]
    [Description("Dry matter weight of dead stems and sheath")]
    [Units("g/m^2")]
    public double leafsenescedwt
    {
        get { return LeafDeadWt * 0.10; }
    }

    /// <summary>Dry matter weight of alive stems and sheath</summary>
    [Output]
    [Description("Dry matter weight of alive stems and sheath")]
    [Units("g/m^2")]
    public double stemsenescedwt
    {
        get { return StemDeadWt * 0.10; }
    }

    /// <summary>Green Cover (needed by SoilWat)</summary>
    [Output]
    [Description("Fraction of soil covered by plant green tissues")]
    [Units("0-1")]
    public double cover_green
    {
        get { return CoverGreen; }
    }

    /// <summary>Total Cover (needed by SWIM and SoilWat)</summary>
    [Output]
    [Description("Fraction of soil covered by plant tissues")]
    [Units("0-1")]
    public double cover_tot
    {
        get { return CoverTotal; }
    }

    /// <summary>Plant N concentration (needed by DDRUles)</summary>
    [Output]
    [Description("N concentration above ground")]
    [Units("%")]
    public double AboveGroundNPct
    {
        get { return AboveGroundNConc * 100.0; }
    }

    ////- N amount outputs >>>  - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
    /// <summary>Total amount of N in the plant</summary>
    [Output]
    [Description("Total amount of N in the plant")]
    [Units("kgN/ha")]
    public double TotalN
    {
        get { return AboveGroundN + BelowGroundN; }
    }

    /// <summary>Amount of N in plant above ground</summary>
    [Output]
    [Description("Amount of N in the plant above ground")]
    [Units("kgN/ha")]
    public double AboveGroundN
    {
        get
        {
            double result = 0.0;
            for (int s = 0; s < NumSpecies; s++)
                result += mySpecies[s].AboveGroundN;
            return result;
        }
    }

    /// <summary>Amount of N in live tissues above ground</summary>
    [Output]
    [Description("Amount of N in live tissues above ground")]
    [Units("kgN/ha")]
    public double AboveGroundLiveN
    {
        get
        {
            double result = 0.0;
            for (int s = 0; s < NumSpecies; s++)
                result += mySpecies[s].AboveGroundLiveN;
            return result;
        }
    }

    /// <summary>Amount of N in dead tissues above ground</summary>
    [Output]
    [Description("Amount of N in dead tissues above ground")]
    [Units("kgN/ha")]
    public double AboveGroundDeadN
    {
        get
        {
            double result = 0.0;
            for (int s = 0; s < NumSpecies; s++)
                result += mySpecies[s].AboveGroundDeadN;
            return result;
        }
    }

    /// <summary>Amount of N in plant below ground</summary>
    [Output]
    [Description("Amount of N in the plant below ground")]
    [Units("kgN/ha")]
    public double BelowGroundN
    {
        get
        {
            double result = 0.0;
            for (int s = 0; s < NumSpecies; s++)
                result += mySpecies[s].roots.NTotal;
            return result;
        }
    }

    /// <summary>Amount of N in standing herbage</summary>
    [Output]
    [Description("Amount of N in standing herbage")]
    [Units("kgN/ha")]
    public double StandingHerbageN
    {
        get
        {
            double result = 0.0;
            for (int s = 0; s < NumSpecies; s++)
                result += mySpecies[s].StandingN;
            return result;
        }
    }

    /// <summary>Amount of N in the plant's leaves</summary>
    [Output]
    [Description("Amount of N in the plant's leaves")]
    [Units("kgN/ha")]
    public double LeafN
    {
        get
        {
            double result = 0.0;
            for (int s = 0; s < NumSpecies; s++)
                result += mySpecies[s].leaves.NTotal;
            return result;
        }
    }

    /// <summary>Amount of N in the plant's stems and sheath</summary>
    [Output]
    [Description("Amount of N in the plant's stems and sheath")]
    [Units("kgN/ha")]
    public double StemN
    {
        get
        {
            double result = 0.0;
            for (int s = 0; s < NumSpecies; s++)
                result += mySpecies[s].stems.NTotal;
            return result;
        }
    }

    /// <summary>Amount of N in the plant's stolons</summary>
    [Output]
    [Description("Amount of N in the plant's stolons")]
    [Units("kgN/ha")]
    public double StolonN
    {
        get
        {
            double result = 0.0;
            for (int s = 0; s < NumSpecies; s++)
                result += mySpecies[s].stolons.NTotal;
            return result;
        }
    }

    /// <summary>Amount of N in the plant's roots</summary>
    [Output]
    [Description("Amount of N in the plant's roots")]
    [Units("kgN/ha")]
    public double RootN
    {
        get
        {
            double result = 0.0;
            for (int s = 0; s < NumSpecies; s++)
                result += mySpecies[s].roots.NGreen;
            return result;
        }
    }

    ////- N concentration outputs >>> - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
    /// <summary>Average N concentration in plant above ground</summary>
    [Output]
    [Description("Average N concentration in the plant above ground")]
    [Units("kgN/kgDM")]
    public double AboveGroundNConc
    {
        get
        {
            double result = 0.0;
            if (AboveGroundWt > 0.0)
                result = AboveGroundN / AboveGroundWt;
            return result;
        }
    }

    /// <summary>Average N concentration in standing herbage</summary>
    [Output]
    [Description("Average N concentration in standing herbage")]
    [Units("kgN/kgDM")]
    public double StandingHerbageNConc
    {
        get
        {
            double result = 0.0;
            if (StandingHerbageWt > 0.0)
                result = StandingHerbageN / StandingHerbageWt;
            return result;
        }
    }

    /// <summary>Average N concentration in plant's leaves</summary>
    [Output]
    [Description("Average N concentration in plant's leaves")]
    [Units("kgN/kgDM")]
    public double LeafNConc
    {
        get
        {
            double result = 0.0;
            if (LeafWt > 0.0)
                result = LeafN / LeafWt;
            return result;
        }
    }

    /// <summary>Average N concentration in plant's stems</summary>
    [Output]
    [Description("Average N concentration in plant's stems")]
    [Units("kgN/kgDM")]
    public double StemNConc
    {
        get
        {
            double result = 0.0;
            if (StemWt > 0.0)
                result = StemN / StemWt;
            return result;
        }
    }

    /// <summary>Average N concentration in plant's stolons</summary>
    [Output]
    [Description("Average N concentration in plant's stolons")]
    [Units("kgN/kgDM")]
    public double StolonNConc
    {
        get
        {
            double result = 0.0;
            if (StolonWt > 0.0)
                result = StolonN / StolonWt;
            return result;
        }
    }

    /// <summary>Average N concentration in plant's roots</summary>
    [Output]
    [Description("Average N concentration in plant's roots")]
    [Units("kgN/kgDM")]
    public double RootNConc
    {
        get
        {
            double result = 0.0;
            if (RootWt > 0.0)
                result = RootN/ RootWt;
            return result;
        }
    }

    ////- DM growth and senescence outputs >>>  - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
    /// <summary>Base potential photosynthetic rate, before damages, in carbon equivalent</summary>
    [Output]
    [Description("Base potential photosynthetic rate, before damages, in carbon equivalent")]
    [Units("kgC/ha")]
    public double BasePotentialPhotosynthesisC
    {
        get
        {
            double result = 0.0;
            for (int s = 0; s < NumSpecies; s++)
                result += mySpecies[s].PotPhoto;
            return result;
        }
    }

    /// <summary>Gross potential photosynthetic rate, after considering damages, in carbon equivalent</summary>
    [Output]
    [Description("Gross potential photosynthetic rate, after considering damages, in carbon equivalent")]
    [Units("kgC/ha")]
    public double GrossPotentialPhotosynthesisC
    {
        get
        {
            double result = 0.0;
            for (int s = 0; s < NumSpecies; s++)
                result += mySpecies[s].Pgross;
            return result;
        }
    }

    /// <summary>Respiration costs expressed in carbon equivalent</summary>
    [Output]
    [Description("Respiration costs expressed in carbon equivalent")]
    [Units("kgC/ha")]
    public double RespirationLossC
    {
        get
        {
            double result = 0.0;
            for (int s = 0; s < NumSpecies; s++)
                result += mySpecies[s].Resp_m + mySpecies[s].Resp_g;
            return result;
        }
    }

    /// <summary>N fixation costs expressed in carbon equivalent</summary>
    [Output]
    [Description("N fixation costs expressed in carbon equivalent")]
    [Units("kgC/ha")]
    public double NFixationCostC
    {
        get
        {
            double result = 0.0;
            for (int s = 0; s < NumSpecies; s++)
                result += mySpecies[s].costNFixation;
            return result;
        }
    }

    /// <summary>Remobilised carbon from senesced tissues</summary>
    [Output]
    [Description("Remobilised carbon from senesced tissues")]
    [Units("kgC/ha")]
    public double RemobilisedSenescedC
    {
        get
        {
            double result = 0.0;
            for (int s = 0; s < NumSpecies; s++)
                result += mySpecies[s].Cremob;
            return result;
        }
    }

    /// <summary>Gross potential growth rate</summary>
    [Output]
    [Description("Gross potential growth rate")]
    [Units("kgDM/ha")]
    public double GrossPotentialGrowthWt
    {
        get
        {
            double result = 0.0;
            for (int s = 0; s < NumSpecies; s++)
                result += mySpecies[s].Pgross / CarbonFractionDM;
            return result;
        }
    }

    /// <summary>Net potential growth rate, after respiration</summary>
    [Output]
    [Description("Net potential growth rate, after respiration")]
    [Units("kgDM/ha")]
    public double NetPotentialGrowthWt
    {
        get { return swardNetPotentialGrowth; }
    }

    /// <summary>Net potential growth rate after water stress</summary>
    [Output]
    [Description("Net potential growth rate after water stress")]
    [Units("kgDM/ha")]
    public double NetPotentialGrowthAfterWaterWt
    {
        get { return swardPotGrowthAfterWater; }
    }

    /// <summary>Net potential growth rate after nutrient stress</summary>
    [Output]
    [Description("Net potential growth rate after nutrient stress")]
    [Units("kgDM/ha")]
    public double NetPotentialGrowthAfterNutrientWt
    {
        get { return swardPotGrowthAfterNutrient; }
    }

    /// <summary>Net, or actual, plant growth rate</summary>
    [Output]
    [Description("Net, or actual, plant growth rate")]
    [Units("kgDM/ha")]
    public double NetGrowthWt
    {
        get { return swardPotGrowthAfterNutrient - swardLitterDM - swardSenescedRootDM; }
    }

    /// <summary>Net herbage growth rate (above ground)</summary>
    [Output]
    [Description("Net herbage growth rate (above ground)")]
    [Units("kgDM/ha")]
    public double HerbageGrowthWt
    {
        get
        {
            double result = 0.0;
            for (int s = 0; s < NumSpecies; s++)
            {
                result += (mySpecies[s].dGrowthShoot - mySpecies[s].dDMLitter);
            }
            return result;
        }
    }

    /// <summary>Net root growth rate</summary>
    [Output]
    [Description("Net root growth rate")]
    [Units("kgDM/ha")]
    public double RootGrowthWt
    {
        get
        {
            double result = 0.0;
            for (int s = 0; s < NumSpecies; s++)
            {
                result += (mySpecies[s].dGrowthRoot - mySpecies[s].dDMRootSen);
            }
            return result;
        }
    }

    /// <summary>Dry matter weight of detached dead material deposited onto soil surface</summary>
    [Output]
    [Description("Dry matter weight of detached dead material deposited onto soil surface")]
    [Units("kgDM/ha")]
    public double LitterDepositionWt
    {
        get { return swardLitterDM; }
    }

    /// <summary>Dry matter weight of detached dead roots added to soil FOM</summary>
    [Output]
    [Description("Dry matter weight of detached dead roots added to soil FOM")]
    [Units("kgDM/ha")]
    public double RootDetachedWt
    {
        get { return swardSenescedRootDM; }
    }

    ////- N flows outputs >>> - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
    /// <summary>Amount of senesced N potentially remobilisable</summary>
    [Output]
    [Description("Amount of senesced N potentially remobilisable")]
    [Units("kgN/ha")]
    public double RemobilisableSenescedN
    {
        get
        {
            double result = 0.0;
            for (int s = 0; s < NumSpecies; s++)
                result += mySpecies[s].leaves.NSenesced + mySpecies[s].stems.NSenesced
                        + mySpecies[s].stolons.NSenesced + mySpecies[s].roots.NSenesced;
            return result;
        }
    }

    /// <summary>Amount of senesced N actually remobilised</summary>
    [Output]
    [Description("Amount of senesced N actually remobilised")]
    [Units("kgN/ha")]
    public double RemobilisedSenescedN
    {
        get
        {
            double result = 0.0;
            for (int s = 0; s < NumSpecies; s++)
                result += mySpecies[s].leaves.NSenescedRemobilised + mySpecies[s].stems.NSenescedRemobilised
                        + mySpecies[s].stolons.NSenescedRemobilised + mySpecies[s].roots.NSenescedRemobilised;
            return result;
        }
    }

    /// <summary>Amount of luxury N potentially remobilisable</summary>
    [Output]
    [Description("Amount of luxury N potentially remobilisable")]
    [Units("kgN/ha")]
    public double RemobilisableLuxuryN
    {
        get
        {
            double result = 0.0;
            for (int s = 0; s < NumSpecies; s++)
                result += mySpecies[s].NLuxuryRemobilisable;
            return result;
        }
    }

    /// <summary>Amount of luxury N actually remobilised</summary>
    [Output]
    [Description("Amount of luxury N actually remobilised")]
    [Units("kgN/ha")]
    public double RemobilisedLuxuryN
    {
        get
        {
            double result = 0.0;
            for (int s = 0; s < NumSpecies; s++)
                result += mySpecies[s].leaves.NLuxuryRemobilised + mySpecies[s].stems.NLuxuryRemobilised
                        + mySpecies[s].stolons.NLuxuryRemobilised + mySpecies[s].roots.NLuxuryRemobilised;
            return result;
        }
    }

    /// <summary>Amount of atmospheric N fixed by symbiosis</summary>
    [Output]
    [Description("Amount of atmospheric N fixed by symbiosis")]
    [Units("kgN/ha")]
    public double FixedN
    {
        get { return swardNFixed; }
    }

    /// <summary>Amount of N required with luxury uptake</summary>
    [Output]
    [Description("Amount of N required with luxury uptake")]
    [Units("kgN/ha")]
    public double DemandAtLuxuryN
    {
        get
        {
            double result = 0.0;
            for (int s = 0; s < NumSpecies; s++)
            {
                result += mySpecies[s].NdemandLux;
            }
            return result;
        }
    }

    /// <summary>Amount of N required for optimum growth</summary>
    [Output]
    [Description("Amount of N required for optimum growth")]
    [Units("kgN/ha")]
    public double DemandAtOptimumN
    {
        get
        {
            double result = 0.0;
            for (int s = 0; s < NumSpecies; s++)
            {
                result += mySpecies[s].NdemandOpt;
            }
            return result;
        }
    }

    /// <summary>Amount of N demanded from the soil</summary>
    [Output]
    [Description("Amount of N demanded from the soil")]
    [Units("kgN/ha")]
    public double SoilDemandN
    {
        get { return swardSoilNDemand; }
    }

    /// <summary>Amount of N available in the soil</summary>
    [Output]
    [Description("Amount of plant available N in the soil")]
    [Units("kgN/ha")]
    public double SoilAvailableN
    {
        get { return swardSoilNavailable; }
    }

    /// <summary>Amount of N available in each soil layer</summary>
    [Output]
    [Description("Amount of plant available N in each soil layer")]
    [Units("kgN/ha")]
    public double[] NSupplyLayers
    {
        get
        {
            double[] result = new double[dlayer.Length];
            for (int layer = 0; layer < dlayer.Length; layer++)
                result[layer] = soilNH4Available[layer] + soilNO3Available[layer];
            return result;
        }
    }

    /// <summary>Amount of N taken up from the soil</summary>
    [Output]
    [Description("Amount of N taken up from the soil")]
    [Units("kgN/ha")]
    public double SoilUptakeN
    {
        get { return soilNH4Uptake.Sum() + soilNO3Uptake.Sum(); }
    }

    /// <summary>Amount of N taken up from each soil layer</summary>
    [Output]
    [Description("Amount of N taken up from each soil layer")]
    [Units("kgN/ha")]
    public double[] NUptakeLayers
    {
        get
        {
            double[] result = new double[dlayer.Length];
            for (int layer = 0; layer < dlayer.Length; layer++)
                result[layer] = soilNH4Uptake[layer] + soilNO3Uptake[layer];
            return result;
        }
    }

    /// <summary>Amount of N in detached dead material deposited onto soil surface</summary>
    [Output]
    [Description("Amount of N in detached dead material deposited onto soil surface")]
    [Units("kgN/ha")]
    public double LitterDepositionN
    {
        get { return swardLitterN; }
    }

    /// <summary>Amount of N in detached dead roots added to soil FOM</summary>
    [Output]
    [Description("Amount of N in detached dead roots added to soil FOM")]
    [Units("kgN/ha")]
    public double RootDetachedN
    {
        get { return swardSenescedRootN; }
    }

    /// <summary>Amount of N in new growth</summary>
    [Output]
    [Description("Amount of N in new growth")]
    [Units("kgN/ha")]
    public double NetGrowthN
    {
        get
        {
            double result = 0.0;
            for (int s = 0; s < NumSpecies; s++)
            {
                result += mySpecies[s].newGrowthN;
            }
            return result;
        }
    }

    ////- Water related outputs >>> - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
    /// <summary>Amount of water demanded by the plants</summary>
    [Output]
    [Description("Amount of water demanded by the plants")]
    [Units("mm")]
    public double WaterDemand
    {
        get { return swardWaterDemand; }
    }

    /// <summary>Amount of plant available water in the soil</summary>
    [Output]
    [Description("Amount of plant available water in the soil")]
    [Units("mm")]
    public double WaterAvailable
    {
        get { return soilAvailableWater.Sum(); }
    }

    /// <summary>Amount of plant available water in each soil layer</summary>
    [Output]
    [Description("Amount of plant available water in each soil layer")]
    [Units("mm")]
    public double[] WaterSupplyLayers
    {
        get { return soilAvailableWater; }
    }

    /// <summary>Amount of water taken up from the soil</summary>
    [Output]
    [Description("Amount of water taken up from the soil")]
    [Units("mm")]
    public double WaterUptake
    {
        get { return soilWaterUptake.Sum(); }
    }

    /// <summary>Amount of water taken up from each soil layer</summary>
    [Output]
    [Description("Amount of water taken up from each soil layer")]
    [Units("mm")]
    public double[] WaterUptakeLayers
    {
        get { return soilWaterUptake; }
    }

    ////- Growth limiting factors >>> - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
    /// <summary>Growth factor due to variations in intercepted radiation</summary>
    [Output]
    [Description("Growth factor due to variations in intercepted radiation")]
    [Units("0-1")]
    public double GlfRadnIntercept
    {
        get
        {
            double result = 1.0;
            if (BasePotentialPhotosynthesisC > Epsilon)
            {
                result = 0.0;
                for (int s = 0; s < NumSpecies; s++)
                {
                    result += mySpecies[s].RadnFactor * mySpecies[s].PotPhoto;
                }

                result /= BasePotentialPhotosynthesisC;
            }
            return result;
        }
    }

    /// <summary>Growth factor due to variations in atmospheric CO2</summary>
    [Output]
    [Description("Growth factor due to variations in atmospheric CO2")]
    [Units("0-1")]
    public double GlfCO2
    {
        get
        {
            double result = 1.0;
            if (BasePotentialPhotosynthesisC > Epsilon)
            {
                result = 0.0;
                for (int s = 0; s < NumSpecies; s++)
                {
                    result += mySpecies[s].CO2Factor * mySpecies[s].PotPhoto;
                }

                result /= BasePotentialPhotosynthesisC;
            }
            return result;
        }
    }

    /// <summary>Growth factor due to variations in plant N concentration</summary>
    [Output]
    [Description("Growth factor due to variations in plant N concentration")]
    [Units("0-1")]
    public double GlfNContent
    {
        get
        {
            double result = 1.0;
            if (BasePotentialPhotosynthesisC > Epsilon)
            {
                result = 0.0;
                for (int s = 0; s < NumSpecies; s++)
                {
                    result += mySpecies[s].NcFactor * mySpecies[s].PotPhoto;
                }
                result /= BasePotentialPhotosynthesisC;
            }
            return result;
        }
    }

    /// <summary>Growth factor due to variations in air temperature</summary>
    [Output]
    [Description("Growth factor due to variations in air temperature")]
    [Units("0-1")]
    public double GlfTemperature
    {
        get
        {
            double result = 1.0;
            if (BasePotentialPhotosynthesisC > Epsilon)
            {
                result = 0.0;
                for (int s = 0; s < NumSpecies; s++)
                {
                    result += mySpecies[s].TempFactor * mySpecies[s].PotPhoto;
                }

                result /= BasePotentialPhotosynthesisC;
            }
            return result;
        }
    }

    /// <summary>Growth factor due to heat damage stress</summary>
    [Output]
    [Description("Growth factor due to heat damage stress")]
    [Units("0-1")]
    public double GlfHeatDamage
    {
        get
        {
            double result = 1.0;
            if (BasePotentialPhotosynthesisC > Epsilon)
            {
                result = 0.0;
                for (int s = 0; s < NumSpecies; s++)
                    result += mySpecies[s].highTempStress * mySpecies[s].PotPhoto;
                result /= BasePotentialPhotosynthesisC;
            }

            return result;
        }
    }

    /// <summary>Growth factor due to cold damage stress</summary>
    [Output]
    [Description("Growth factor due to cold damage stress")]
    [Units("0-1")]
    public double GlfColdDamage
    {
        get
        {
            double result = 1.0;
            if (BasePotentialPhotosynthesisC > Epsilon)
            {
                result = 0.0;
                for (int s = 0; s < NumSpecies; s++)
                    result += mySpecies[s].lowTempStress * mySpecies[s].PotPhoto;
                result /= BasePotentialPhotosynthesisC;
            }

            return result;
        }
    }

    /// <summary>Generic limiting factor for photosynthesis, user set</summary>
    [Output]
    [Description("Generic limiting factor for photosynthesis, user set")]
    [Units("0-1")]
    public double GlfGeneric
    {
        get
        {
            double result = 1.0;
            if (BasePotentialPhotosynthesisC > Epsilon)
            {
                result = 0.0;
                for (int s = 0; s < NumSpecies; s++)
                {
                    result += mySpecies[s].GLFGeneric * mySpecies[s].PotPhoto;
                }
                result /= BasePotentialPhotosynthesisC;
            }
            return result;
        }
    }

    private double frgr = 1.0;

    /// <summary>Growth limiting factor due to water deficit</summary>
    [Output]
    [Description("Growth limiting factor due to water deficit")]
    [Units("0-1")]
    public double GlfWaterSupply
    {
        get { return swardGLFWater; }
    }

    /// <summary>Growth limiting factor due to water logging</summary>
    [Output]
    [Description("Growth limiting factor due to water logging")]
    [Units("0-1")]
    public double GlfWaterLogging
    {
        get { return swardGLFWLogging; }
    }

    /// <summary>Growth limiting factor due to soil N availability</summary>
    [Output]
    [Description("Growth limiting factor due to soil N availability")]
    [Units("0-1")]
    public double GlfNSupply
    {
        get { return swardGLFN; }
    }

    /// <summary>Generic growth limiting factor due to soil fertility, user set</summary>
    [Output]
    [Description("Generic growth limiting factor due to soil fertility, user set")]
    [Units("0-1")]
    public double GlfSoilFertility
    {
        get
        {
            double result = 1.0;
            if (NetPotentialGrowthAfterWaterWt > Epsilon)
            {
                result = 0.0;
                for (int s = 0; s < NumSpecies; s++)
                {
                    result += mySpecies[s].GLFSFertility * mySpecies[s].dGrowthW;
                }

                result /= NetPotentialGrowthAfterWaterWt;
            }
            return result;
        }
    }

    /// <summary>Plant relative growth rate, sent to micromet</summary>
    [Output]
    [Description("Plant relative growth rate, sent to micromet")]
    [Units("0-1")]
    public double Frgr
    {
        get { return frgr; }
    }

    /// <summary>Effect of vapour pressure on growth (used by micromet)</summary>
    [Output]
    [Description("Effect of vapour pressure on growth (used by micromet)")]
    [Units("0-1")]
    public double FVPD
    {
        // mostly = 1 for crop/grass/forage
        get { return FVPDFunction.Value(VPD()); }
    }

    /// <summary>Temperature factor for respiration</summary>
    [Output]
    [Description("Temperature factor for respiration")]
    [Units("0-1")]
    public double TemperatureFactorRespiration
    {
        get
        {
            double result = 1.0;
            if (AboveGroundLiveWt > Epsilon)
            {
                result = 0.0;
                for (int s = 0; s < NumSpecies; s++)
                {
                    result += mySpecies[s].tempFactorRespiration * mySpecies[s].AboveGroundLiveWt;
                }

                result /= AboveGroundLiveWt;
            }
            return result;
        }
    }

    ////- DM allocation and turnover rates >>>  - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
    /// <summary>Fraction of new growth allocated to shoot</summary>
    [Output]
    [Description("Fraction of new growth allocated to shoot")]
    [Units("0-1")]
    public double FractionGrowthToShoot
    {
        get
        {
            double result = 0.0;
            if (swardPotGrowthAfterNutrient > 0.0)
                result = HerbageGrowthWt / swardPotGrowthAfterNutrient;
            return result;
        }
    }

    /// <summary>Fraction of new growth allocated to roots</summary>
    [Output]
    [Description("Fraction of new growth allocated to roots")]
    [Units("0-1")]
    public double FractionGrowthToRoot
    {
        get
        {
            double result = 0.0;
            if (swardPotGrowthAfterNutrient > 0.0)
                result = RootGrowthWt / swardPotGrowthAfterNutrient;
            return result;
        }
    }

    ////- LAI and cover outputs >>> - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
    /// <summary>Leaf area index of green tissues</summary>
    [Output]
    [Description("Leaf area index of green tissues")]
    [Units("m^2/m^2")]
    public double LAIGreen
    {
        get
        {
            double result = 0.0;
            for (int s = 0; s < NumSpecies; s++)
                result += mySpecies[s].greenLAI;
            return result;
        }
    }

    /// <summary>Leaf area index of dead tissues</summary>
    [Output]
    [Description("Leaf area index of dead tissues")]
    [Units("m^2/m^2")]
    public double LAIDead
    {
        get
        {
            double result = 0.0;
            for (int s = 0; s < NumSpecies; s++)
                result += mySpecies[s].deadLAI;
            return result;
        }
    }

    /// <summary>Total leaf area index</summary>
    [Output]
    [Description("Total leaf area index")]
    [Units("m^2/m^2")]
    public double LAITotal
    {
        get { return LAIGreen + LAIDead; }
    }

    /// <summary>Fraction of soil covered by green tissues</summary>
    [Output]
    [Description("Fraction of soil covered by green tissues")]
    [Units("0-1")]
    public double CoverGreen
    {
        get
        {
            if (LAIGreen == 0) return 0;
            return 1.0 - Math.Exp(-swardLightExtCoeff * LAIGreen);
        }
    }

    /// <summary>Fraction of soil covered by dead tissues</summary>
    [Output]
    [Description("Fraction of soil covered by dead tissues")]
    [Units("0-1")]
    public double CoverDead
    {
        get
        {
            if (LAIDead == 0) return 0;
            return 1.0 - Math.Exp(-swardLightExtCoeff * LAIDead);
        }
    }

    /// <summary>Fraction of soil covered by plant tissues</summary>
    [Output]
    [Description("Fraction of soil covered by plant tissues")]
    [Units("0-1")]
    public double CoverTotal
    {
        get
        {
            if (LAITotal == 0) return 0;
            return 1.0 - (Math.Exp(-swardLightExtCoeff * LAITotal));
        }
    }

    /// <summary>Average light extinction coefficient of sward</summary>
    [Output]
    [Description("Average light extinction coefficient of sward")]
    [Units("0-1")]
    public double LightExtinctionCoefficient
    {
        get { return swardLightExtCoeff; }
    }

    /// <summary>Solar radiation intercepted by whole sward</summary>
    [Output]
    [Description("Solar radiation intercepted by whole sward")]
    [Units("MJ/m^2/day")]
    public double InterceptedRadn;

    ////- Height and root depth >>> - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
    /// <summary>Average height of plants in the sward</summary>
    [Output]
    [Description("Average height of plants in the sward")] //needed by micromet
    [Units("mm")]
    public double Height
    {
        get
        {
            double result = 0.0;
            if (AboveGroundWt > 0)
            {
                for (int s = 0; s < NumSpecies; s++)
                    result += mySpecies[s].height * mySpecies[s].AboveGroundWt;

                result /= AboveGroundWt;
            }

            return result;
        }
    }

    /// <summary>Average depth of root zone in the sward</summary>
    [Output]
    [Description("Average depth of root zone in the sward")]
    [Units("mm")]
    public double RootDepth
    {
        get { return swardRootDepth; }
    }

    /// <summary>Fraction of root dry matter for each soil layer</summary>
    [Output]
    [Description("Fraction of root dry matter for each soil layer")]
    [Units("0-1")]
    public double[] RootWtFraction
    {
        get { return swardRootFraction; }
    }

    /// <summary>Root length density by volume</summary>
    [Output]
    [Description("Root length density by volume")]
    [Units("mm/mm^3")]
    public double[] rlv
    {
        get
        {
            //Compute the root length, total over the whole profile
            double[] result = new double[dlayer.Length];
            double Total_Rlength = 0.0;
            for (int layer = 0; layer < result.Length; layer++)
            {
                Total_Rlength = 0.0;
                if (usingSpeciesRoot)
                {
                    for (int s = 0; s < NumSpecies; s++)
                        Total_Rlength += (mySpecies[s].roots.DMGreen * 0.1) * mySpecies[s].rootFraction[layer] * mySpecies[s].SpecificRootLength;
                }
                else
                    Total_Rlength += (BelowGroundWt * 0.1) * swardRootFraction[layer] * mySpecies[0].SpecificRootLength;

                // average root length (m root/m2 soil)
                result[layer] = Total_Rlength / (dlayer[layer] * 1000); // mm root/mm3 soil
            }
            return result;
        }
    }

    ////- Harvest outputs >>> - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
    /// <summary>Dry matter weight available for harvesting</summary>
    [Output]
    [Description("Dry matter weight available for harvesting")]
    [Units("kgDM/ha")]
    public double HarvestableWt
    {
        get
        {
            double result = 0.0;
            for (int s = 0; s < NumSpecies; s++)
                result += mySpecies[s].leaves.DMTotalHarvestable + mySpecies[s].stems.DMTotalHarvestable + mySpecies[s].stolons.DMTotalHarvestable;
            return result;
        }
    }

    /// <summary>Amount of plant dry matter removed by harvest</summary>
    [Output]
    [Description("Amount of plant dry matter removed by harvest")]
    [Units("kgDM/ha")]
    public double HarvestedWt
    {
        get
        {
            double result = 0.0;
            for (int s = 0; s < NumSpecies; s++)
                result += mySpecies[s].dmdefoliated;
            return result;
        }
    }

    /// <summary>Amount of plant N removed by harvest</summary>
    [Output]
    [Description("Amount of plant N removed by harvest")]
    [Units("kgN/ha")]
    public double HarvestedN
    {
        get
        {
            double result = 0.0;
            if (HarvestedWt > 0.0)
            {
                for (int s = 0; s < NumSpecies; s++)
                    result += mySpecies[s].Ndefoliated;
            }
            return result;
        }
    }

    /// <summary>Average N concentration in harvested material</summary>
    [Output]
    [Description("Average N concentration in harvested material")]
    [Units("kgN/kgDM")]
    public double HarvestedNConc
    {
        get
        {
            double result = 0.0;
            if (HarvestedWt > 0.0)
                result = HarvestedN / HarvestedWt;
            return result;
        }
    }

    /// <summary>Average digestibility of harvested material</summary>
    [Output]
    [Description("Average digestibility of harvested material")]
    [Units("0-1")]
    public double HarvestedDigestibility
    {
        get { return swardHarvestDigestibility; }
    }

    /// <summary>Average metabolisable energy concentration of harvested material</summary>
    [Output]
    [Description("Average metabolisable energy concentration of harvested material")]
    [Units("MJ/kgDM")]
    public double HarvestedME
    {
        get
        {
            double result = PotentialMEOfHerbage * HarvestedDigestibility;
            return result;
        }
    }

    /// <summary>Average digestibility of standing herbage</summary>
    [Output]
    [Description("Average digestibility of standing herbage")]
    [Units("0-1")]
    public double HerbageDigestibility
    {
        get
        {
            double result = 0.0;
            if (isAlive && (StandingHerbageWt > Epsilon))
                for (int s = 0; s < NumSpecies; s++)
                    result += mySpecies[s].digestHerbage * mySpecies[s].StandingWt / StandingHerbageWt;
            return result;
        }
    }

    /// <summary>Average metabolisable energy concentration of standing herbage</summary>
    [Output]
    [Description("Average metabolisable energy concentration of standing herbage")]
    [Units("MJ/kgDM")]
    public double HerbageME
    {
        get
        {
            double result = PotentialMEOfHerbage * HerbageDigestibility;
            return result;
        }
    }

    ////- Useful derived outputs >>>  - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
    /// <summary>Gross primary productivity</summary>
    [Output]
    [Description("Gross primary productivity")]
    [Units("kgC/ha")]
    public double GPP
    {
        get
        {
            double result = 0.0;
            for (int s = 0; s < NumSpecies; s++)
                result += mySpecies[s].Pgross;
            return result;
        }
    }

    /// <summary>Net primary productivity</summary>
    [Output]
    [Description("Net primary productivity")]
    [Units("kgC/ha")]
    public double NPP
    {
        get
        {
            double result = 0.0;
            for (int s = 0; s < NumSpecies; s++)
                result += (mySpecies[s].Pgross * mySpecies[s].growthEfficiency) - mySpecies[s].Resp_m;
            return result;
        }
    }

    /// <summary>Net above-ground primary productivity</summary>
    [Output]
    [Description("Net above-ground primary productivity")]
    [Units("kgC/ha")]
    public double NAPP
    {
        get
        {
            double result = 0.0;
            for (int s = 0; s < NumSpecies; s++)
                result += ((mySpecies[s].Pgross * mySpecies[s].growthEfficiency) - mySpecies[s].Resp_m) *
                          mySpecies[s].ShootAllocationFactor;
            return result;
        }
    }

    /// <summary>Net below-ground primary productivity</summary>
    [Output]
    [Description("Net below-ground primary productivity")]
    [Units("kgC/ha")]
    public double NBPP
    {
        get
        {
            double result = 0.0;
            for (int s = 0; s < NumSpecies; s++)
                result += ((mySpecies[s].Pgross * mySpecies[s].growthEfficiency) - mySpecies[s].Resp_m) *
                          (1.0 - mySpecies[s].ShootAllocationFactor);
            return result;
        }
    }

    #endregion  - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -

    #region - Species Outputs - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -

    ////- General properties >>>  - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -

    /// <summary>Generic crop type of each species</summary>
    [Output]
    [Description("Generic crop type of each species")]
    [Units("")]
    public string[] SpeciesCrop_type
    {
        get { return micrometType; }
    }

    /// <summary>Name of each species</summary>
    [Output]
    [Description("Name of each species")]
    [Units("")]
    public string[] Species_name
    {
        get
        {
            string[] result = new string[NumSpecies];
            for (int s = 0; s < NumSpecies; s++)
                result[s] = mySpecies[s].speciesName;
            return result;
        }
    }

    ////- DM and C outputs >>>  - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
    /// <summary>Total amount of C in the plant, for each species</summary>
    [Output]
    [Description("Total amount of C in the plant, for each species")]
    [Units("kgC/ha")]
    public double[] SpeciesTotalC
    {
        get
        {
            double[] result = new double[mySpecies.Length];
            for (int s = 0; s < NumSpecies; s++)
                result[s] = (mySpecies[s].AboveGroundWt + mySpecies[s].roots.DMTotal) * CarbonFractionDM;
            return result;
        }
    }

    /// <summary>Total dry matter weight of plant, for each species</summary>
    [Output]
    [Description("Total dry matter weight of plant, for each species")]
    [Units("kgDM/ha")]
    public double[] SpeciesTotalWt
    {
        get
        {
            double[] result = new double[mySpecies.Length];
            for (int s = 0; s < NumSpecies; s++)
                result[s] = mySpecies[s].AboveGroundWt + mySpecies[s].roots.DMTotal;
            return result;
        }
    }

    /// <summary>Dry matter weight of plant above ground, for each species</summary>
    [Output]
    [Description("Dry matter weight of plant above ground, for each species")]
    [Units("kgDM/ha")]
    public double[] SpeciesAboveGroundWt
    {
        get
        {
            double[] result = new double[mySpecies.Length];
            for (int s = 0; s < NumSpecies; s++)
                result[s] = mySpecies[s].AboveGroundWt;
            return result;
        }
    }

    /// <summary>Dry matter weight of plant below ground, for each species</summary>
    [Output]
    [Description("Dry matter weight of plant below ground, for each species")]
    [Units("kgDM/ha")]
    public double[] SpeciesBelowGroundWt
    {
        get
        {
            double[] result = new double[mySpecies.Length];
            for (int s = 0; s < NumSpecies; s++)
                result[s] = mySpecies[s].roots.DMTotal;
            return result;
        }
    }

    /// <summary>Dry matter weight of standing herbage, for each species</summary>
    [Output]
    [Description("Dry matter weight of standing herbage, for each species")]
    [Units("kgDM/ha")]
    public double[] SpeciesStandingHerbageWt
    {
        get
        {
            double[] result = new double[mySpecies.Length];
            for (int s = 0; s < NumSpecies; s++)
                result[s] = mySpecies[s].StandingWt;
            return result;
        }
    }

    /// <summary>Dry matter weight of live standing herbage, for each species</summary>
    [Output]
    [Description("Dry matter weight of live standing herbage, for each species")]
    [Units("kgDM/ha")]
    public double[] SpeciesStandingLiveHerbageWt
    {
        get
        {
            double[] result = new double[mySpecies.Length];
            for (int s = 0; s < NumSpecies; s++)
                result[s] = mySpecies[s].StandingLiveWt;
            return result;
        }
    }

    /// <summary>Dry matter weight of dead standing herbage, for each species</summary>
    [Output]
    [Description("Dry matter weight of dead standing herbage, for each species")]
    [Units("kgDM/ha")]
    public double[] SpeciesStandingDeadHerbageWt
    {
        get
        {
            double[] result = new double[mySpecies.Length];
            for (int s = 0; s < NumSpecies; s++)
                result[s] = mySpecies[s].StandingDeadWt;
            return result;
        }
    }

    /// <summary>Dry matter weight of plant's leaves, for each species</summary>
    [Output]
    [Description("Dry matter weight of plant's leaves, for each species")]
    [Units("kgDM/ha")]
    public double[] SpeciesLeafWt
    {
        get
        {
            double[] result = new double[mySpecies.Length];
            for (int s = 0; s < NumSpecies; s++)
                result[s] = mySpecies[s].leaves.DMTotal;
            return result;
        }
    }

    /// <summary>Dry matter weight of plant's stems and sheath, for each species</summary>
    [Output]
    [Description("Dry matter weight of plant's stems and sheath, for each species")]
    [Units("kgDM/ha")]
    public double[] SpeciesStemWt
    {
        get
        {
            double[] result = new double[mySpecies.Length];
            for (int s = 0; s < NumSpecies; s++)
                result[s] = mySpecies[s].stems.DMTotal;
            return result;
        }
    }

    /// <summary>Dry matter weight of plant's stolons, for each species</summary>
    [Output]
    [Description("Dry matter weight of plant's stolons, for each species")]
    [Units("kgDM/ha")]
    public double[] SpeciesStolonWt
    {
        get
        {
            double[] result = new double[mySpecies.Length];
            for (int s = 0; s < NumSpecies; s++)
                result[s] = mySpecies[s].stolons.DMGreen;
            return result;
        }
    }

    /// <summary>Dry matter weight of plant's roots, for each species</summary>
    [Output]
    [Description("Dry matter weight of plant's roots, for each species")]
    [Units("kgDM/ha")]
    public double[] SpeciesRootWt
    {
        get
        {
            double[] result = new double[mySpecies.Length];
            for (int s = 0; s < NumSpecies; s++)
                result[s] = mySpecies[s].roots.DMGreen;
            return result;
        }
    }

    ////- N amount outputs >>>  - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
    /// <summary>Total amount of N in the plant, for each species</summary>
    [Output]
    [Description("Total amount of N in the plant, for each species")]
    [Units("kgN/ha")]
    public double[] SpeciesTotalN
    {
        get
        {
            double[] result = new double[mySpecies.Length];
            for (int s = 0; s < NumSpecies; s++)
                result[s] = mySpecies[s].AboveGroundN + mySpecies[s].roots.NTotal;
            return result;
        }
    }

    /// <summary>Amount of N in plant above ground, for each species</summary>
    [Output]
    [Description("Amount of N in plant above ground, for each species")]
    [Units("kgN/ha")]
    public double[] SpeciesAboveGroundN
    {
        get
        {
            double[] result = new double[mySpecies.Length];
            for (int s = 0; s < NumSpecies; s++)
                result[s] = mySpecies[s].AboveGroundN;
            return result;
        }
    }

    /// <summary>Amount of N in plant below ground, for each species</summary>
    [Output]
    [Description("Amount of N in plant below ground, for each species")]
    [Units("kgN/ha")]
    public double[] SpeciesBelowGroundN
    {
        get
        {
            double[] result = new double[mySpecies.Length];
            for (int s = 0; s < NumSpecies; s++)
                result[s] = mySpecies[s].roots.NTotal;
            return result;
        }
    }

    /// <summary>Amount of N in standing herbage, for each species</summary>
    [Output]
    [Description("Amount of N in standing herbage, for each species")]
    [Units("kgN/ha")]
    public double[] SpeciesStandingHerbageN
    {
        get
        {
            double[] result = new double[mySpecies.Length];
            for (int s = 0; s < NumSpecies; s++)
                result[s] = mySpecies[s].StandingN;
            return result;
        }
    }

    /// <summary>Amount of N in the plant's leaves, for each species</summary>
    [Output]
    [Description("Amount of N in the plant's leaves, for each species")]
    [Units("kgN/ha")]
    public double[] SpeciesLeafN
    {
        get
        {
            double[] result = new double[mySpecies.Length];
            for (int s = 0; s < NumSpecies; s++)
                result[s] = mySpecies[s].leaves.NTotal;
            return result;
        }
    }

    /// <summary>Amount of N in the plant's stems and sheath, for each species</summary>
    [Output]
    [Description("Amount of N in the plant's stems and sheath, for each species")]
    [Units("kgN/ha")]
    public double[] SpeciesStemN
    {
        get
        {
            double[] result = new double[mySpecies.Length];
            for (int s = 0; s < NumSpecies; s++)
                result[s] = mySpecies[s].stems.NTotal;
            return result;
        }
    }

    /// <summary>Amount of N in the plant's stolons, for each species</summary>
    [Output]
    [Description("Amount of N in the plant's stolons, for each species")]
    [Units("kgN/ha")]
    public double[] SpeciesStolonN
    {
        get
        {
            double[] result = new double[mySpecies.Length];
            for (int s = 0; s < NumSpecies; s++)
                result[s] = mySpecies[s].stolons.DMGreen;
            return result;
        }
    }

    /// <summary>Amount of N in the plant's roots, for each species</summary>
    [Output]
    [Description("Amount of N in the plant's roots, for each species")]
    [Units("kgN/ha")]
    public double[] SpeciesRootN
    {
        get
        {
            double[] result = new double[mySpecies.Length];
            for (int s = 0; s < NumSpecies; s++)
                result[s] = mySpecies[s].roots.NGreen;
            return result;
        }
    }

    ////- N concentration outputs >>> - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
    /// <summary>Average N concentration in plant above ground, for each species</summary>
    [Output]
    [Description("Average N concentration in plant above ground, for each species")]
    [Units("kgN/kgDM")]
    public double[] SpeciesAboveGroundNConc
    {
        get
        {
            double[] result = new double[mySpecies.Length];
            for (int s = 0; s < NumSpecies; s++)
            {
                result[s] = MathUtility.Divide(mySpecies[s].AboveGroundN, mySpecies[s].AboveGroundWt, 0.0);
            }
            return result;
        }
    }

    /// <summary>Average N concentration in standing herbage, for each species</summary>
    [Output]
    [Description("Average N concentration in standing herbage, for each species")]
    [Units("kgN/kgDM")]
    public double[] SpeciesStandingHerbageNConc
    {
        get
        {
            double[] result = new double[mySpecies.Length];
            for (int s = 0; s < NumSpecies; s++)
                result[s] = MathUtility.Divide(mySpecies[s].StandingN, mySpecies[s].StandingWt, 0.0);
            return result;
        }
    }

    /// <summary>Average N concentration in plant's leaves, for each species</summary>
    [Output]
    [Description("Average N concentration in plant's leaves, for each species")]
    [Units("kgN/kgDM")]
    public double[] SpeciesLeafNConc
    {
        get
        {
            double[] result = new double[mySpecies.Length];
            for (int s = 0; s < NumSpecies; s++)
            {
                result[s] = mySpecies[s].leaves.NconcTotal;
            }
            return result;
        }
    }

    /// <summary>Average N concentration in plant's stems, for each species</summary>
    [Output]
    [Description("Average N concentration in plant's stems, for each species")]
    [Units("kgN/kgDM")]
    public double[] SpeciesStemNConc
    {
        get
        {
            double[] result = new double[mySpecies.Length];
            for (int s = 0; s < NumSpecies; s++)
            {
                result[s] = mySpecies[s].stems.NconcTotal;
            }
            return result;
        }
    }

    /// <summary>Average N concentration in plant's stolons, for each species</summary>
    [Output]
    [Description("Average N concentration in plant's stolons, for each species")]
    [Units("kgN/kgDM")]
    public double[] SpeciesStolonNConc
    {
        get
        {
            double[] result = new double[mySpecies.Length];
            for (int s = 0; s < NumSpecies; s++)
            {
                result[s] = mySpecies[s].stolons.NconcGreen;
            }
            return result;
        }
    }

    /// <summary>Average N concentration in plant's roots, for each species</summary>
    [Output]
    [Description("Average N concentration in plant's roots, for each species")]
    [Units("kgN/kgDM")]
    public double[] SpeciesRootNConc
    {
        get
        {
            double[] result = new double[mySpecies.Length];
            for (int s = 0; s < NumSpecies; s++)
            {
                result[s] = mySpecies[s].roots.NconcGreen;
            }
            return result;
        }
    }

    ////- DM growth and senescence outputs >>>  - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
    /// <summary>Base potential photosynthetic rate, before damages, in carbon equivalent, for each species</summary>
    [Output]
    [Description("Base potential photosynthetic rate, before damages, in carbon equivalent, for each species")]
    [Units("kgC/ha")]
    public double[] SpeciesBasePotentialPhotosynthesisC
    {
        get
        {
            double[] result = new double[mySpecies.Length];
            for (int s = 0; s < NumSpecies; s++)
                result[s] = mySpecies[s].PotPhoto;
            return result;
        }
    }

    /// <summary>Gross potential photosynthetic rate, after considering damages, in carbon equivalent, for each species</summary>
    [Output]
    [Description("Gross potential photosynthetic rate, after considering damages, in carbon equivalent, for each species")]
    [Units("kgC/ha")]
    public double[] SpeciesGrossPotentialPhotosynthesisC
    {
        get
        {
            double[] result = new double[mySpecies.Length];
            for (int s = 0; s < NumSpecies; s++)
                result[s] = mySpecies[s].Pgross;
            return result;
        }
    }

    /// <summary>Respiration costs expressed in carbon equivalent, for each species</summary>
    [Output]
    [Description("Respiration costs expressed in carbon equivalent, for each species")]
    [Units("kgC/ha")]
    public double[] SpeciesRespirationLossC
    {
        get
        {
            double[] result = new double[mySpecies.Length];
            for (int s = 0; s < NumSpecies; s++)
                result[s] = mySpecies[s].Resp_m + mySpecies[s].Resp_g;
            return result;
        }
    }

    /// <summary>N fixation costs expressed in carbon equivalent, for each species</summary>
    [Output]
    [Description("N fixation costs expressed in carbon equivalent, for each species")]
    [Units("kgC/ha")]
    public double[] SpeciesNFixationCostC
    {
        get
        {
            double[] result = new double[mySpecies.Length];
            for (int s = 0; s < NumSpecies; s++)
                result[s] = mySpecies[s].costNFixation;
            return result;
        }
    }

    /// <summary>Gross potential growth rate, for each species</summary>
    [Output]
    [Description("Gross potential growth rate, for each species")]
    [Units("kgDM/ha")]
    public double[] SpeciesGrossPotentialGrowthWt
    {
        get
        {
            double[] result = new double[NumSpecies];
            for (int s = 0; s < NumSpecies; s++)
                result[s] = mySpecies[s].Pgross / CarbonFractionDM;
            return result;
        }
    }

    /// <summary>Net potential growth rate, after respiration, for each species</summary>
    [Output]
    [Description("Net potential growth rate, after respiration, for each species")]
    [Units("kgDM/ha")]
    public double[] SpeciesNetPotentialGrowthWt
    {
        get
        {
            double[] result = new double[NumSpecies];
            for (int s = 0; s < NumSpecies; s++)
                result[s] = mySpecies[s].dGrowthPot;
            return result;
        }
    }

    /// <summary>Net potential growth rate after water stress, for each species</summary>
    [Output]
    [Description("Net potential growth rate after water stress, for each species")]
    [Units("kgDM/ha")]
    public double[] SpeciesNetPotentialGrowthAfterWaterWt
    {
        get
        {
            double[] result = new double[NumSpecies];
            for (int s = 0; s < NumSpecies; s++)
                result[s] = mySpecies[s].dGrowthW;
            return result;
        }
    }

    /// <summary>Net potential growth rate after nutrient stress, for each species</summary>
    [Output]
    [Description("Net potential growth rate after nutrient stress, for each species")]
    [Units("kgDM/ha")]
    public double[] SpeciesNetPotentialGrowthAfterNutrientWt
    {
        get
        {
            double[] result = new double[NumSpecies];
            for (int s = 0; s < NumSpecies; s++)
                result[s] = mySpecies[s].dGrowth;
            return result;
        }
    }

    /// <summary>Net, or actual, plant growth rate, for each species</summary>
    [Output]
    [Description("Net, or actual, plant growth rate, for each species")]
    [Units("kgDM/ha")]
    public double[] SpeciesNetGrowthWt
    {
        get
        {
            double[] result = new double[NumSpecies];
            for (int s = 0; s < NumSpecies; s++)
                result[s] = mySpecies[s].dGrowth - mySpecies[s].dDMLitter - mySpecies[s].dDMRootSen;
            return result;
        }
    }

    /// <summary>Net herbage growth rate (above ground), for each species</summary>
    [Output]
    [Description("Net herbage growth rate (above ground), for each species")]
    [Units("kgDM/ha")]
    public double[] SpeciesHerbageGrowthWt
    {
        get
        {
            double[] result = new double[NumSpecies];
            for (int s = 0; s < NumSpecies; s++)
                result[s] = mySpecies[s].dGrowthShoot - mySpecies[s].dDMLitter;
            return result;
        }
    }

    /// <summary>Net root growth rate, for each species</summary>
    [Output]
    [Description("Net root growth rate, for each species")]
    [Units("kgDM/ha")]
    public double[] SpeciesRootGrowthWt
    {
        get
        {
            double[] result = new double[NumSpecies];
            for (int s = 0; s < NumSpecies; s++)
                result[s] = mySpecies[s].dGrowthRoot - mySpecies[s].dDMRootSen;
            return result;
        }
    }

    /// <summary>Dry matter weight of detached dead material deposited onto soil surface, for each species</summary>
    [Output]
    [Description("Dry matter weight of detached dead material deposited onto soil surface, for each species")]
    [Units("kgDM/ha")]
    public double[] SpeciesLitterDepositionWt
    {
        get
        {
            double[] result = new double[NumSpecies];
            for (int s = 0; s < NumSpecies; s++)
                result[s] = mySpecies[s].dDMLitter;
            return result;
        }
    }

    /// <summary>Dry matter weight of detached dead roots added to soil FOM, for each species</summary>
    [Output]
    [Description("Dry matter weight of detached dead roots added to soil FOM, for each species")]
    [Units("kgDM/ha")]
    public double[] SpeciesRootDetachedWt
    {
        get
        {
            double[] result = new double[NumSpecies];
            for (int s = 0; s < NumSpecies; s++)
                result[s] = mySpecies[s].dDMRootSen;
            return result;
        }
    }

    ////- N flows outputs >>> - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
    /// <summary>Amount of senesced N potentially remobilisable, for each species</summary>
    [Output]
    [Description("Amount of senesced N potentially remobilisable, for each species")]
    [Units("kgN/ha")]
    public double[] SpeciesRemobilisableSenescedN
    {
        get
        {
            double[] result = new double[mySpecies.Length];
            for (int s = 0; s < NumSpecies; s++)
            {
                result[s] = mySpecies[s].leaves.NSenesced + mySpecies[s].stems.NSenesced
                            + mySpecies[s].stolons.NSenesced + mySpecies[s].roots.NSenesced;
            }
            return result;
        }
    }

    /// <summary>Amount of senesced N actually remobilised, for each species</summary>
    [Output]
    [Description("Amount of senesced N actually remobilised, for each species")]
    [Units("kgN/ha")]
    public double[] SpeciesRemobilisedSenescedN
    {
        get
        {
            double[] result = new double[mySpecies.Length];
            for (int s = 0; s < NumSpecies; s++)
            {
                result[s] = mySpecies[s].leaves.NSenescedRemobilised + mySpecies[s].stems.NSenescedRemobilised
                            + mySpecies[s].stolons.NSenescedRemobilised + mySpecies[s].roots.NSenescedRemobilised;
            }
            return result;
        }
    }

    /// <summary>Amount of luxury N potentially remobilisable, for each species</summary>
    [Output]
    [Description("Amount of luxury N potentially remobilisable, for each species")]
    [Units("kgN/ha")]
    public double[] SpeciesRemobilisableLuxuryN
    {
        get
        {
            double[] result = new double[mySpecies.Length];
            for (int s = 0; s < NumSpecies; s++)
                result[s] = mySpecies[s].NLuxuryRemobilisable;
            return result;
        }
    }

    /// <summary>Amount of luxury N actually remobilised, for each species</summary>
    [Output]
    [Description("Amount of luxury N actually remobilised, for each species")]
    [Units("kgN/ha")]
    public double[] SpeciesRemobilisedLuxuryN
    {
        get
        {
            double[] result = new double[mySpecies.Length];
            for (int s = 0; s < NumSpecies; s++)
            {
                result[s] = mySpecies[s].leaves.NLuxuryRemobilised + mySpecies[s].stems.NLuxuryRemobilised
                            + mySpecies[s].stolons.NLuxuryRemobilised + mySpecies[s].roots.NLuxuryRemobilised;
            }
            return result;
        }
    }

    /// <summary>Amount of atmospheric N fixed by symbiosis, for each species</summary>
    [Output]
    [Description("Amount of atmospheric N fixed by symbiosis, for each species")]
    [Units("kgN/ha")]
    public double[] SpeciesFixedN
    {
        get
        {
            double[] result = new double[mySpecies.Length];
            for (int s = 0; s < NumSpecies; s++)
            {
                result[s] = mySpecies[s].NFixed;
            }
            return result;
        }
    }

    /// <summary>Amount of N required with luxury uptake, for each species</summary>
    [Output]
    [Description("Amount of N required with luxury uptake, for each species")]
    [Units("kgN/ha")]
    public double[] SpeciesDemandAtLuxuryN
    {
        get
        {
            double[] result = new double[mySpecies.Length];
            for (int s = 0; s < NumSpecies; s++)
            {
                result[s] = mySpecies[s].NdemandLux;
            }
            return result;
        }
    }

    /// <summary>Amount of N required for optimum growth, for each species</summary>
    [Output]
    [Description("Amount of N required for optimum growth, for each species")]
    [Units("kgN/ha")]
    public double[] SpeciesDemandAtOptimumN
    {
        get
        {
            double[] result = new double[mySpecies.Length];
            for (int s = 0; s < NumSpecies; s++)
            {
                result[s] = mySpecies[s].NdemandOpt;
            }
            return result;
        }
    }

    /// <summary>Amount of N demanded from the soil, for each species</summary>
    [Output]
    [Description("Amount of N demanded from the soil, for each species")]
    [Units("kgN/ha")]
    public double[] SpeciesSoilDemandN
    {
        get
        {
            double[] result = new double[mySpecies.Length];
            for (int s = 0; s < NumSpecies; s++)
            {
                result[s] = mySpecies[s].soilNdemand;
            }
            return result;
        }
    }

    /// <summary>Amount of plant available N in the soil, for each species</summary>
    [Output]
    [Description("Amount of plant available N in the soil, for each species")]
    [Units("kgN/ha")]
    public double[] SpeciesSoilAvailableN
    {
        get
        {
            double[] result = new double[mySpecies.Length];
            for (int s = 0; s < NumSpecies; s++)
            {
                result[s] = mySpecies[s].soilAvailableNH4.Sum() + mySpecies[s].soilAvailableNO3.Sum();
            }
            return result;
        }
    }

    /// <summary>Amount of N taken up from the soil, for each species</summary>
    [Output]
    [Description("Amount of N taken up from the soil, for each species")]
    [Units("kgN/ha")]
    public double[] SpeciesSoilUptakeN
    {
        get
        {
            double[] result = new double[mySpecies.Length];
            for (int s = 0; s < NumSpecies; s++)
            {
                result[s] = mySpecies[s].soilNH4Uptake + mySpecies[s].soilNO3Uptake;
            }
            return result;
        }
    }

    /// <summary>Amount of N in detached dead material deposited onto soil surface, for each species</summary>
    [Output]
    [Description("Amount of N in detached dead material deposited onto soil surface, for each species")]
    [Units("kgN/ha")]
    public double[] SpeciesLitterDepositionN
    {
        get
        {
            double[] result = new double[mySpecies.Length];
            for (int s = 0; s < NumSpecies; s++)
            {
                result[s] = mySpecies[s].dNLitter;
            }
            return result;
        }
    }

    /// <summary>Amount of N in detached dead roots added to soil FOM, for each species</summary>
    [Output]
    [Description("Amount of N in detached dead roots added to soil FOM, for each species")]
    [Units("kgN/ha")]
    public double[] SpeciesRootDetachedN
    {
        get
        {
            double[] result = new double[mySpecies.Length];
            for (int s = 0; s < NumSpecies; s++)
            {
                result[s] = mySpecies[s].dNRootSen;
            }
            return result;
        }
    }

    /// <summary>Amount of N in new growth, for each species</summary>
    [Output]
    [Description("Amount of N in new growth, for each species")]
    [Units("kgN/ha")]
    public double[] SpeciesNetGrowthN
    {
        get
        {
            double[] result = new double[mySpecies.Length];
            for (int s = 0; s < NumSpecies; s++)
            {
                result[s] = mySpecies[s].newGrowthN;
            }
            return result;
        }
    }

    ////- Water related outputs >>> - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
    /// <summary>Amount of water demanded by the plant, for each species</summary>
    [Output]
    [Description("Amount of water demanded by the plant, for each species")]
    [Units("mm")]
    public double[] SpeciesWaterDemand
    {
        get
        {
            double[] result = new double[mySpecies.Length];
            for (int s = 0; s < NumSpecies; s++)
                result[s] = mySpecies[s].WaterDemand;
            return result;
        }
    }

    /// <summary>Amount of plant available water in the soil, for each species</summary>
    [Output]
    [Description("Amount of plant available water in the soil, for each species")]
    [Units("mm")]
    public double[] SpeciesWaterAvailable
    {
        get
        {
            double[] result = new double[mySpecies.Length];
            for (int s = 0; s < NumSpecies; s++)
                result[s] = mySpecies[s].soilAvailableWater.Sum();
            return result;
        }
    }

    /// <summary>Amount of water taken up from the soil, for each species</summary>
    [Output]
    [Description("Amount of water taken up from the soil, for each species")]
    [Units("mm")]
    public double[] SpeciesWaterUptake
    {
        get
        {
            double[] result = new double[mySpecies.Length];
            for (int s = 0; s < NumSpecies; s++)
                result[s] = mySpecies[s].soilWaterUptake.Sum();
            return result;
        }
    }

    ////- Growth limiting factors >>> - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
    /// <summary>Fraction of radiation intercepted by each species</summary>
    [Output]
    [Description("Fraction of radiation intercepted by each species")]
    [Units("0-1")]
    public double[] SpeciesCanopyRadnFraction
    {
        get
        {
            double[] result = new double[mySpecies.Length];
            for (int s = 0; s < NumSpecies; s++)
                result[s] = mySpecies[s].canopyCompetitionFactor;
            return result;
        }
    }

    /// <summary>Growth factor due to variations in intercepted radiation, for each species</summary>
    [Output]
    [Description("Growth factor due to variations in intercepted radiation, for each species")]
    [Units("0-1")]
    public double[] SpeciesGlfRadnIntercept
    {
        get
        {
            double[] result = new double[mySpecies.Length];
            for (int s = 0; s < NumSpecies; s++)
                result[s] = mySpecies[s].RadnFactor;
            return result;
        }
    }

    /// <summary>Growth factor due to variations in atmospheric CO2, for each species</summary>
    [Output]
    [Description("Growth factor due to variations in atmospheric CO2, for each species")]
    [Units("")]
    public double[] SpeciesGlfCO2
    {
        get
        {
            double[] result = new double[mySpecies.Length];
            for (int s = 0; s < NumSpecies; s++)
                result[s] = mySpecies[s].CO2Factor;
            return result;
        }
    }

    /// <summary>Growth factor due to variations in plant N concentration, for each species</summary>
    [Output]
    [Description("Growth factor due to variations in plant N concentration, for each species")]
    [Units("0-1")]
    public double[] SpeciesGlfNContent
    {
        get
        {
            double[] result = new double[mySpecies.Length];
            for (int s = 0; s < NumSpecies; s++)
                result[s] = mySpecies[s].NcFactor;
            return result;
        }
    }

    /// <summary>Growth factor due to variations in air temperature, for each species</summary>
    [Output]
    [Description("Growth factor due to variations in air temperature, for each species")]
    [Units("0-1")]
    public double[] SpeciesGlfTemperature
    {
        get
        {
            double[] result = new double[mySpecies.Length];
            for (int s = 0; s < NumSpecies; s++)
                result[s] = mySpecies[s].TempFactor;
            return result;
        }
    }

    /// <summary>Growth factor due to heat damage stress, for each species</summary>
    [Output]
    [Description("Growth factor due to heat damage stress, for each species")]
    [Units("0-1")]
    public double[] SpeciesGlfHeatDamage
    {
        get
        {
            double[] result = new double[mySpecies.Length];
            for (int s = 0; s < NumSpecies; s++)
                result[s] = mySpecies[s].highTempStress;
            return result;
        }
    }

    /// <summary>Growth factor due to cold damage stress, for each species</summary>
    [Output]
    [Description("Growth factor due to cold damage stress, for each species")]
    [Units("0-1")]
    public double[] SpeciesGlfColdDamage
    {
        get
        {
            double[] result = new double[mySpecies.Length];
            for (int s = 0; s < NumSpecies; s++)
                result[s] = mySpecies[s].lowTempStress;
            return result;
        }
    }

    /// <summary>Generic limiting factor for photosynthesis, user set, for each species</summary>
    [Output]
    [Description("Generic limiting factor for photosynthesis, user set, for each species")]
    [Units("0-1")]
    public double[] SpeciesGlfGeneric
    {
        get
        {
            double[] result = new double[mySpecies.Length];
            for (int s = 0; s < NumSpecies; s++)
                result[s] = mySpecies[s].GLFGeneric;
            return result;
        }
    }

    /// <summary>Growth limiting factor due to water deficit, for each species</summary>
    [Output]
    [Description("Growth limiting factor due to water deficit, for each species")]
    [Units("0-1")]
    public double[] SpeciesGlfWaterSupply
    {
        get
        {
            double[] result = new double[mySpecies.Length];
            for (int s = 0; s < NumSpecies; s++)
                result[s] = mySpecies[s].glfWater;
            return result;
        }
    }

    /// <summary>Growth limiting factor due to water logging, for each species</summary>
    [Output]
    [Description("Growth limiting factor due to water logging, for each species")]
    [Units("0-1")]
    public double[] SpeciesGlfWaterLogging
    {
        get
        {
            double[] result = new double[mySpecies.Length];
            for (int s = 0; s < NumSpecies; s++)
                result[s] = mySpecies[s].glfAeration;
            return result;
        }
    }

    /// <summary>Growth limiting factor due to soil N availability, for each species</summary>
    [Output]
    [Description("Growth limiting factor due to soil N availability, for each species")]
    [Units("0-1")]
    public double[] SpeciesGlfNSupply
    {
        get
        {
            double[] result = new double[mySpecies.Length];
            for (int s = 0; s < NumSpecies; s++)
                result[s] = mySpecies[s].glfN;
            return result;
        }
    }

    /// <summary>Generic growth limiting factor due to soil fertility, user set, for each species</summary>
    [Output]
    [Description("Generic growth limiting factor due to soil fertility, user set, for each species")]
    [Units("0-1")]
    public double[] SpeciesGlfSoilFertility
    {
        get
        {
            double[] result = new double[mySpecies.Length];
            for (int s = 0; s < NumSpecies; s++)
                result[s] = mySpecies[s].GLFSFertility;
            return result;
        }
    }

    /// <summary>Temperature factor for respiration, for each species</summary>
    [Output]
    [Description("Temperature factor for respiration, for each species")]
    [Units("0-1")]
    public double[] SpeciesTemperatureFactorRespiration
    {
        get
        {
            double[] result = new double[mySpecies.Length];
            for (int s = 0; s < NumSpecies; s++)
                result[s] = mySpecies[s].tempFactorRespiration;
            return result;
        }
    }

    ////- DM allocation and turnover rates >>>  - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
    /// <summary>Fraction of new growth allocated to shoot, for each species</summary>
    [Output]
    [Description("Fraction of new growth allocated to shoot, for each species")]
    [Units("0-1")]
    public double[] SpeciesFractionGrowthToShoot
    {
        get
        {
            double[] result = new double[NumSpecies];
            for (int s = 0; s < NumSpecies; s++)
                result[s] = mySpecies[s].ShootAllocationFactor;
            return result;
        }
    }

    /// <summary>Fraction of new shoot growth allocated to leaves, for each species</summary>
    [Output]
    [Description("Fraction of new shoot growth allocated to leaves, for each species")]
    [Units("0-1")]
    public double[] SpeciesFractionGrowthToLeaf
    {
        get
        {
            double[] result = new double[NumSpecies];
            for (int s = 0; s < NumSpecies; s++)
                result[s] = mySpecies[s].LeafAllocationFactor;
            return result;
        }
    }

    /// <summary>Turnover rate for live shoot tissues (leaves and stem), for each species</summary>
    [Output]
    [Description("Turnover rate for live shoot tissues (leaves and stem), for each species")]
    [Units("0-1")]
    public double[] SpeciesTurnoverRateLiveShoot
    {
        get
        {
            double[] result = new double[mySpecies.Length];
            for (int s = 0; s < NumSpecies; s++)
            {
                result[s] = mySpecies[s].gama;
            }
            return result;
        }
    }

    /// <summary>Turnover rate for dead shoot tissues (leaves and stem), for each species</summary>
    [Output]
    [Description("Turnover rate for dead shoot tissues (leaves and stem), for each species")]
    [Units("0-1")]
    public double[] SpeciesTurnoverRateDeadShoot
    {
        get
        {
            double[] result = new double[mySpecies.Length];
            for (int s = 0; s < NumSpecies; s++)
            {
                result[s] = mySpecies[s].gamaD;
            }
            return result;
        }
    }

    /// <summary>Turnover rate for stolon tissues, for each species</summary>
    [Output]
    [Description("Turnover rate for stolon tissues, for each species")]
    [Units("0-1")]
    public double[] SpeciesTurnoverRateStolons
    {
        get
        {
            double[] result = new double[mySpecies.Length];
            for (int s = 0; s < NumSpecies; s++)
            {
                result[s] = mySpecies[s].gamaS;
            }
            return result;
        }
    }

    /// <summary>Turnover rate for roots tissues, for each species</summary>
    [Output]
    [Description("Turnover rate for roots tissues, for each species")]
    [Units("0-1")]
    public double[] SpeciesTurnoverRateRoots
    {
        get
        {
            double[] result = new double[mySpecies.Length];
            for (int s = 0; s < NumSpecies; s++)
            {
                result[s] = mySpecies[s].gamaR;
            }
            return result;
        }
    }

    /// <summary>Temperature factor for tissue turnover, for each species</summary>
    [Output]
    [Description("Temperature factor for tissue turnover, for each species")]
    [Units("0-1")]
    public double[] SpeciesTemperatureFactorTurnover
    {
        get
        {
            double[] result = new double[mySpecies.Length];
            for (int s = 0; s < NumSpecies; s++)
                result[s] = mySpecies[s].tempFacTTurnover;
            return result;
        }
    }

    /// <summary>Moisture factor for tissue turnover, for each species</summary>
    [Output]
    [Description("Moisture factor for tissue turnover, for each species")]
    [Units("0-1")]
    public double[] SpeciesMoistureFactorTurnover
    {
        get
        {
            double[] result = new double[mySpecies.Length];
            for (int s = 0; s < NumSpecies; s++)
                result[s] = mySpecies[s].swFacTTurnover;
            return result;
        }
    }

    ////- LAI and cover outputs >>> - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
    /// <summary>Leaf area index of green tissues, for each species</summary>
    [Output]
    [Description("Leaf area index of green tissues, for each species")]
    [Units("m^2/m^2")]
    public double[] SpeciesLAIGreen
    {
        get
        {
            double[] result = new double[mySpecies.Length];
            for (int s = 0; s < NumSpecies; s++)
                result[s] = mySpecies[s].greenLAI;
            return result;
        }
    }

    /// <summary>Leaf area index of dead tissues, for each species</summary>
    [Output]
    [Description("Leaf area index of dead tissues, for each species")]
    [Units("m^2/m^2")]
    public double[] SpeciesLAIDead
    {
        get
        {
            double[] result = new double[mySpecies.Length];
            for (int s = 0; s < NumSpecies; s++)
                result[s] = mySpecies[s].deadLAI;
            return result;
        }
    }

    /// <summary>Total leaf area index, for each species</summary>
    [Output]
    [Description("Total leaf area index, for each species")]
    [Units("m^2/m^2")]
    public double[] SpeciesLAITotal
    {
        get
        {
            double[] result = new double[mySpecies.Length];
            for (int s = 0; s < NumSpecies; s++)
                result[s] = mySpecies[s].totalLAI;
            return result;
        }
    }

    /// <summary>Fraction of soil covered by green tissues, for each species</summary>
    [Output]
    [Description("Fraction of soil covered by green tissues, for each species")]
    [Units("0-1")]
    public double[] SpeciesCoverGreen
    {
        get
        {
            double[] result = new double[mySpecies.Length];
            for (int s = 0; s < NumSpecies; s++)
                result[s] = mySpecies[s].coverGreen;
            return result;
        }
    }

    /// <summary>Fraction of soil covered by dead tissues, for each species</summary>
    [Output]
    [Description("Fraction of soil covered by dead tissues, for each species")]
    [Units("0-1")]
    public double[] SpeciesCoverDead
    {
        get
        {
            double[] result = new double[mySpecies.Length];
            for (int s = 0; s < NumSpecies; s++)
                result[s] = mySpecies[s].coverDead;
            return result;
        }
    }

    /// <summary>Fraction of soil covered by plant tissues, for each species</summary>
    [Output]
    [Description("Fraction of soil covered by plant tissues, for each species")]
    [Units("0-1")]
    public double[] SpeciesCoverTotal
    {
        get
        {
            double[] result = new double[mySpecies.Length];
            for (int s = 0; s < NumSpecies; s++)
                result[s] = mySpecies[s].coverTotal;
            return result;
        }
    }

    /// <summary>Solar radiation intercepted by the plant, for each species</summary>
    [Output]
    [Description("Solar radiation intercepted by the plant, for each species")]
    [Units("MJ/m^2/day")]
    public double[] SpeciesInterceptedRadn
    {
        get
        {
            double[] result = new double[NumSpecies];
            for (int s = 0; s < NumSpecies; s++)
                result[s] = mySpecies[s].interceptedRadn;
            return result;
        }
    }

    ////- Height and root depth >>> - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
    /// <summary>Average canopy height, for each species</summary>
    [Output]
    [Description("Average canopy height, for each species")]
    [Units("mm")]
    public double[] SpeciesHeight
    {
        get
        {
            double[] result = new double[NumSpecies];
            for (int s = 0; s < NumSpecies; s++)
                result[s] = mySpecies[s].height;
            return result;
        }
    }

    /// <summary>Average depth of root zone, for each species</summary>
    [Output]
    [Description("Average depth of root zone, for each species")]
    [Units("mm")]
    public double[] SpeciesRootDepth
    {
        get
        {
            double[] result = new double[NumSpecies];
            for (int s = 0; s < NumSpecies; s++)
                result[s] = mySpecies[s].rootDepth;
            return result;
        }
    }

    ////- Harvest outputs >>> - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
    /// <summary>Dry matter weight available for harvesting, for each species</summary>
    [Output]
    [Description("Dry matter weight available for harvesting, for each species")]
    [Units("kgDM/ha")]
    public double[] SpeciesHarvestableWt
    {
        get
        {
            double[] result = new double[NumSpecies];
            for (int s = 0; s < NumSpecies; s++)
                result[s] = mySpecies[s].leaves.DMTotalHarvestable + mySpecies[s].stems.DMTotalHarvestable + mySpecies[s].stolons.DMTotalHarvestable;
            return result;
        }
    }

    /// <summary>Amount of plant dry matter removed by harvest, for each species</summary>
    [Output]
    [Description("Amount of plant dry matter removed by harvest, for each species")]
    [Units("kgDM/ha")]
    public double[] SpeciesHarvestedWt
    {
        get
        {
            double[] result = new double[mySpecies.Length];
            for (int s = 0; s < NumSpecies; s++)
                result[s] = mySpecies[s].dmdefoliated;
            return result;
        }
    }

    /// <summary>Fraction of available dry matter actually harvested, for each species</summary>
    [Output]
    [Description("Fraction of available dry matter actually harvested, for each species")]
    [Units("0-1")]
    public double[] SpeciesHarvestedFraction
    {
        get
        {
            double[] result = new double[mySpecies.Length];
            if (HarvestedWt > 0.0)
                for (int s = 0; s < NumSpecies; s++)
                    result[s] = mySpecies[s].fractionDefoliated;
            return result;
        }
    }

    /// <summary>Amount of plant N removed by harvest, for each species</summary>
    [Output]
    [Description("Amount of plant N removed by harvest, for each species")]
    [Units("kgN/ha")]
    public double[] SpeciesHarvestedN
    {
        get
        {
            double[] result = new double[mySpecies.Length];
            for (int s = 0; s < NumSpecies; s++)
                result[s] = mySpecies[s].Ndefoliated;
            return result;
        }
    }

    /// <summary>Average N concentration in harvested material, for each species</summary>
    [Output]
    [Description("Average N concentration in harvested material, for each species")]
    [Units("kgN/kgDM")]
    public double[] SpeciesHarvestedNConc
    {
        get
        {
            double[] result = new double[mySpecies.Length];
            for (int s = 0; s < NumSpecies; s++)
                if (mySpecies[s].dmdefoliated > 0.0)
                    result[s] = mySpecies[s].Ndefoliated / mySpecies[s].dmdefoliated;
            return result;
        }
    }

    /// <summary>Average digestibility of harvested material, for each species</summary>
    [Output]
    [Description("Average digestibility of harvested material, for each species")]
    [Units("0-1")]
    public double[] SpeciesHarvestedDigestibility
    {
        get
        {
            double[] result = new double[mySpecies.Length];
            for (int s = 0; s < NumSpecies; s++)
                result[s] = mySpecies[s].digestDefoliated;
            return result;
        }
    }

    /// <summary>Average metabolisable energy concentration of harvested material, for each species</summary>
    [Output]
    [Description("Average metabolisable energy concentration of harvested material, for each species")]
    [Units("MJ/kgDM")]
    public double[] SpeciesHarvestedME
    {
        get
        {
            double[] result = new double[mySpecies.Length];
            for (int s = 0; s < NumSpecies; s++)
                result[s] = PotentialMEOfHerbage * mySpecies[s].digestDefoliated;
            return result;
        }
    }

    /// <summary>Dry matter fraction for each species in harvested material</summary>
    [Output]
    [Description("Dry matter fraction for each species in harvested material")]
    [Units("0-1")]
    public double[] SpeciesProportionHarvest
    {
        get
        {
            double[] result = new double[mySpecies.Length];
            if (HarvestedWt > 0.0)
                for (int s = 0; s < NumSpecies; s++)
                    result[s] = mySpecies[s].dmdefoliated / HarvestedWt;
            return result;
        }
    }

    /// <summary>Average digestibility of standing herbage, for each species</summary>
    [Output]
    [Description("Average digestibility of standing herbage, for each species")]
    [Units("0-1")]
    public double[] SpeciesHerbageDigestibility
    {
        get
        {
            double[] result = new double[mySpecies.Length];
            for (int s = 0; s < NumSpecies; s++)
                result[s] = mySpecies[s].digestHerbage;
            return result;
        }
    }

    /// <summary>Average metabolisable energy concentration of standing herbage, for each species</summary>
    [Output]
    [Description("Average metabolisable energy concentration of standing herbage, for each species")]
    [Units("MJ/kgDM")]
    public double[] SpeciesHerbageME
    {
        get
        {
            double[] result = new double[mySpecies.Length];
            for (int s = 0; s < NumSpecies; s++)
                result[s] = PotentialMEOfHerbage * mySpecies[s].digestHerbage;
            return result;
        }
    }

    /// <summary>Dry matter fraction of each species in the standing herbage</summary>
    [Output]
    [Description("Dry matter fraction of each species in the standing herbage")]
    [Units("0-1")]
    public double[] SpeciesProportionStanding
    {
        get
        {
            double[] result = new double[mySpecies.Length];
            double myTotal = StandingHerbageWt;
            if (myTotal > 0.0)
                for (int s = 0; s < NumSpecies; s++)
                    result[s] = mySpecies[s].StandingWt / myTotal;
            return result;
        }
    }

    #endregion  - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -

    #region - Tissue outputs  - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -

    ////- DM outputs >>>  - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
    /// <summary>Dry matter weight of emerging tissues from all above ground organs</summary>
    [Output]
    [Description("Dry matter weight of emerging tissues from all above ground organs")]
    [Units("kgDM/ha")]
    public double EmergingTissuesWt
    {
        get
        {
            double result = 0.0;
            for (int s = 0; s < NumSpecies; s++)
                result += mySpecies[s].leaves.tissue[0].DM + mySpecies[s].stems.tissue[0].DM + mySpecies[s].stolons.tissue[0].DM;
            return result;
        }
    }

    /// <summary>Dry matter weight of developing tissues from all above ground organs</summary>
    [Output]
    [Description("Dry matter weight of developing tissues from all above ground organs")]
    [Units("kgDM/ha")]
    public double DevelopingTissuesWt
    {
        get
        {
            double result = 0.0;
            for (int s = 0; s < NumSpecies; s++)
                result += mySpecies[s].leaves.tissue[1].DM + mySpecies[s].stems.tissue[1].DM + mySpecies[s].stolons.tissue[1].DM;
            return result;
        }
    }

    /// <summary>Dry matter weight of mature tissues from all above ground organs</summary>
    [Output]
    [Description("Dry matter weight of mature tissues from all above ground organs")]
    [Units("kgDM/ha")]
    public double MatureTissuesWt
    {
        get
        {
            double result = 0.0;
            for (int s = 0; s < NumSpecies; s++)
                result += mySpecies[s].leaves.tissue[2].DM + mySpecies[s].stems.tissue[2].DM + mySpecies[s].stolons.tissue[2].DM;
            return result;
        }
    }

    /// <summary>Dry matter weight of dead tissues from all above ground organs</summary>
    [Output]
    [Description("Dry matter weight of dead tissues from all above ground organs")]
    [Units("kgDM/ha")]
    public double DeadTissuesWt
    {
        get
        {
            double result = 0.0;
            for (int s = 0; s < NumSpecies; s++)
                result += mySpecies[s].leaves.tissue[3].DM + mySpecies[s].stems.tissue[3].DM;
            return result;
        }
    }

    /// <summary>Dry matter weight of emerging tissues of plant's leaves</summary>
    [Output]
    [Description("Dry matter weight of emerging tissues of plant's leaves")]
    [Units("kgDM/ha")]
    public double[] SpeciesLeafStage1Wt
    {
        get
        {
            double[] result = new double[mySpecies.Length];
            for (int s = 0; s < NumSpecies; s++)
                result[s] = mySpecies[s].leaves.tissue[0].DM;
            return result;
        }
    }

    /// <summary>Dry matter weight of developing tissues of plant's leaves</summary>
    [Output]
    [Description("Dry matter weight of developing tissues of plant's leaves")]
    [Units("kgDM/ha")]
    public double[] SpeciesLeafStage2Wt
    {
        get
        {
            double[] result = new double[mySpecies.Length];
            for (int s = 0; s < NumSpecies; s++)
                result[s] = mySpecies[s].leaves.tissue[1].DM;
            return result;
        }
    }

    /// <summary>Dry matter weight of mature tissues of plant's leaves</summary>
    [Output]
    [Description("Dry matter weight of mature tissues of plant's leaves")]
    [Units("kgDM/ha")]
    public double[] SpeciesLeafStage3Wt
    {
        get
        {
            double[] result = new double[mySpecies.Length];
            for (int s = 0; s < NumSpecies; s++)
                result[s] = mySpecies[s].leaves.tissue[2].DM;
            return result;
        }
    }

    /// <summary>Dry matter weight of dead tissues of plant's leaves</summary>
    [Output]
    [Description("Dry matter weight of dead tissues of plant's leaves")]
    [Units("kgDM/ha")]
    public double[] SpeciesLeafStage4Wt
    {
        get
        {
            double[] result = new double[mySpecies.Length];
            for (int s = 0; s < NumSpecies; s++)
                result[s] = mySpecies[s].leaves.tissue[3].DM;
            return result;
        }
    }

    /// <summary>Dry matter weight of emerging tissues of plant's stems</summary>
    [Output]
    [Description("Dry matter weight of emerging tissues of plant's stems")]
    [Units("kgDM/ha")]
    public double[] SpeciesStemStage1Wt
    {
        get
        {
            double[] result = new double[mySpecies.Length];
            for (int s = 0; s < NumSpecies; s++)
                result[s] = mySpecies[s].stems.tissue[0].DM;
            return result;
        }
    }

    /// <summary>Dry matter weight of developing tissues of plant's stems</summary>
    [Output]
    [Description("Dry matter weight of developing tissues of plant's stems")]
    [Units("kgDM/ha")]
    public double[] SpeciesStemStage2Wt
    {
        get
        {
            double[] result = new double[mySpecies.Length];
            for (int s = 0; s < NumSpecies; s++)
                result[s] = mySpecies[s].stems.tissue[1].DM;
            return result;
        }
    }

    /// <summary>Dry matter weight of mature tissues of plant's stems</summary>
    [Output]
    [Description("Dry matter weight of mature tissues of plant's stems")]
    [Units("kgDM/ha")]
    public double[] SpeciesStemStage3Wt
    {
        get
        {
            double[] result = new double[mySpecies.Length];
            for (int s = 0; s < NumSpecies; s++)
                result[s] = mySpecies[s].stems.tissue[2].DM;
            return result;
        }
    }

    /// <summary>Dry matter weight of dead tissues of plant's stems</summary>
    [Output]
    [Description("Dry matter weight of dead tissues of plant's stems")]
    [Units("kgDM/ha")]
    public double[] SpeciesStemStage4Wt
    {
        get
        {
            double[] result = new double[mySpecies.Length];
            for (int s = 0; s < NumSpecies; s++)
                result[s] = mySpecies[s].stems.tissue[3].DM;
            return result;
        }
    }

    /// <summary>Dry matter weight of emerging tissues of plant's stolons</summary>
    [Output]
    [Description("Dry matter weight of emerging tissues of plant's stolons")]
    [Units("kgDM/ha")]
    public double[] SpeciesStolonStage1Wt
    {
        get
        {
            double[] result = new double[mySpecies.Length];
            for (int s = 0; s < NumSpecies; s++)
                result[s] = mySpecies[s].stolons.tissue[0].DM;
            return result;
        }
    }

    /// <summary>Dry matter weight of developing tissues of plant's stolons</summary>
    [Output]
    [Description("Dry matter weight of developing tissues of plant's stolons")]
    [Units("kgDM/ha")]
    public double[] SpeciesStolonStage2Wt
    {
        get
        {
            double[] result = new double[mySpecies.Length];
            for (int s = 0; s < NumSpecies; s++)
                result[s] = mySpecies[s].stolons.tissue[1].DM;
            return result;
        }
    }

    /// <summary>Dry matter weight of mature tissues of plant's stolons</summary>
    [Output]
    [Description("Dry matter weight of mature tissues of plant's stolons")]
    [Units("kgDM/ha")]
    public double[] SpeciesStolonStage3Wt
    {
        get
        {
            double[] result = new double[mySpecies.Length];
            for (int s = 0; s < NumSpecies; s++)
                result[s] = mySpecies[s].stolons.tissue[2].DM;
            return result;
        }
    }

    ////- N amount outputs >>>  - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
    /// <summary>Amount of N in emerging tissues from all above ground organs</summary>
    [Output]
    [Description("Amount of N in emerging tissues from all above ground organs")]
    [Units("kgN/ha")]
    public double EmergingTissuesN
    {
        get
        {
            double result = 0.0;
            for (int s = 0; s < NumSpecies; s++)
                result += mySpecies[s].leaves.tissue[0].Namount + mySpecies[s].stems.tissue[0].Namount + mySpecies[s].stolons.tissue[0].Namount;
            return result;
        }
    }

    /// <summary>Amount of N in developing tissues from all above ground organs</summary>
    [Output]
    [Description("Amount of N in developing tissues from all above ground organs")]
    [Units("kgN/ha")]
    public double DevelopingTissuesN
    {
        get
        {
            double result = 0.0;
            for (int s = 0; s < NumSpecies; s++)
                result += mySpecies[s].leaves.tissue[1].Namount + mySpecies[s].stems.tissue[1].Namount + mySpecies[s].stolons.tissue[1].Namount;
            return result;
        }
    }

    /// <summary>Amount of N in mature tissues from all above ground organs</summary>
    [Output]
    [Description("Amount of N in mature tissues from all above ground organs")]
    [Units("kgN/ha")]
    public double MatureTissuesN
    {
        get
        {
            double result = 0.0;
            for (int s = 0; s < NumSpecies; s++)
                result += mySpecies[s].leaves.tissue[2].Namount + mySpecies[s].stems.tissue[2].Namount + mySpecies[s].stolons.tissue[2].Namount;
            return result;
        }
    }

    /// <summary>Amount of N in dead tissues from all above ground organs</summary>
    [Output]
    [Description("Amount of N in dead tissues from all above ground organs")]
    [Units("kgN/ha")]
    public double DeadTissuesN
    {
        get
        {
            double result = 0.0;
            for (int s = 0; s < NumSpecies; s++)
                result += mySpecies[s].leaves.tissue[3].Namount + mySpecies[s].stems.tissue[3].Namount;
            return result;
        }
    }

    /// <summary>Amount of N in emerging tissues of plant's leaves</summary>
    [Output]
    [Description("Amount of N in emerging tissues of plant's leaves")]
    [Units("kgN/ha")]
    public double[] SpeciesLeafStage1N
    {
        get
        {
            double[] result = new double[mySpecies.Length];
            for (int s = 0; s < NumSpecies; s++)
                result[s] = mySpecies[s].leaves.tissue[0].Namount;
            return result;
        }
    }

    /// <summary>Amount of N in developing tissues of plant's leaves</summary>
    [Output]
    [Description("Amount of N in developing tissues of plant's leaves")]
    [Units("kgN/ha")]
    public double[] SpeciesLeafStage2N
    {
        get
        {
            double[] result = new double[mySpecies.Length];
            for (int s = 0; s < NumSpecies; s++)
                result[s] = mySpecies[s].leaves.tissue[1].Namount;
            return result;
        }
    }

    /// <summary>Amount of N in mature tissues of plant's leaves</summary>
    [Output]
    [Description("Amount of N in mature tissues of plant's leaves")]
    [Units("kgN/ha")]
    public double[] SpeciesLeafStage3N
    {
        get
        {
            double[] result = new double[mySpecies.Length];
            for (int s = 0; s < NumSpecies; s++)
                result[s] = mySpecies[s].leaves.tissue[2].Namount;
            return result;
        }
    }

    /// <summary>Amount of N in dead tissues of plant's leaves</summary>
    [Output]
    [Description("Amount of N in dead tissues of plant's leaves")]
    [Units("kgN/ha")]
    public double[] SpeciesLeafStage4N
    {
        get
        {
            double[] result = new double[mySpecies.Length];
            for (int s = 0; s < NumSpecies; s++)
                result[s] = mySpecies[s].leaves.tissue[3].Namount;
            return result;
        }
    }

    /// <summary>Amount of N in emerging tissues of plant's stems</summary>
    [Output]
    [Description("Amount of N in emerging tissues of plant's stems")]
    [Units("kgN/ha")]
    public double[] SpeciesStemStage1N
    {
        get
        {
            double[] result = new double[mySpecies.Length];
            for (int s = 0; s < NumSpecies; s++)
                result[s] = mySpecies[s].stems.tissue[0].Namount;
            return result;
        }
    }

    /// <summary>Amount of N in developing tissues of plant's stems</summary>
    [Output]
    [Description("Amount of N in developing tissues of plant's stems")]
    [Units("kgN/ha")]
    public double[] SpeciesStemStage2N
    {
        get
        {
            double[] result = new double[mySpecies.Length];
            for (int s = 0; s < NumSpecies; s++)
                result[s] = mySpecies[s].stems.tissue[1].Namount;
            return result;
        }
    }

    /// <summary>Amount of N in mature tissues of plant's stems</summary>
    [Output]
    [Description("Amount of N in mature tissues of plant's stems")]
    [Units("kgN/ha")]
    public double[] SpeciesStemStage3N
    {
        get
        {
            double[] result = new double[mySpecies.Length];
            for (int s = 0; s < NumSpecies; s++)
                result[s] = mySpecies[s].stems.tissue[2].Namount;
            return result;
        }
    }

    /// <summary>Amount of N in dead tissues of plant's stems</summary>
    [Output]
    [Description("Amount of N in dead tissues of plant's stems")]
    [Units("kgN/ha")]
    public double[] SpeciesStemStage4N
    {
        get
        {
            double[] result = new double[mySpecies.Length];
            for (int s = 0; s < NumSpecies; s++)
                result[s] = mySpecies[s].stems.tissue[3].Namount;
            return result;
        }
    }

    /// <summary>Amount of N in emerging tissues of plant's stolons</summary>
    [Output]
    [Description("Amount of N in emerging tissues of plant's stolons")]
    [Units("kgN/ha")]
    public double[] SpeciesStolonStage1N
    {
        get
        {
            double[] result = new double[mySpecies.Length];
            for (int s = 0; s < NumSpecies; s++)
                result[s] = mySpecies[s].stolons.tissue[0].Namount;
            return result;
        }
    }

    /// <summary>Amount of N in developing tissues of plant's stolons</summary>
    [Output]
    [Description("Amount of N in developing tissues of plant's stolons")]
    [Units("kgN/ha")]
    public double[] SpeciesStolonStage2N
    {
        get
        {
            double[] result = new double[mySpecies.Length];
            for (int s = 0; s < NumSpecies; s++)
                result[s] = mySpecies[s].stolons.tissue[1].Namount;
            return result;
        }
    }

    /// <summary>Amount of N in mature tissues of plant's stolons</summary>
    [Output]
    [Description("Amount of N in mature tissues of plant's stolons")]
    [Units("kgN/ha")]
    public double[] SpeciesStolonStage3N
    {
        get
        {
            double[] result = new double[mySpecies.Length];
            for (int s = 0; s < NumSpecies; s++)
                result[s] = mySpecies[s].stolons.tissue[2].Namount;
            return result;
        }
    }

    ////- N concentration outputs >>> - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
    /// <summary>N concentration in emerging tissues of plant's leaves</summary>
    [Output]
    [Description("N concentration in emerging tissues of plant's leaves")]
    [Units("kgN/kgDM")]
    public double[] SpeciesLeafStage1NConc
    {
        get
        {
            double[] result = new double[mySpecies.Length];
            for (int s = 0; s < NumSpecies; s++)
                result[s] = mySpecies[s].leaves.tissue[0].Nconc;
            return result;
        }
    }

    /// <summary>N concentration in developing tissues of plant's leaves</summary>
    [Output]
    [Description("N concentration in developing tissues of plant's leaves")]
    [Units("kgN/kgDM")]
    public double[] SpeciesLeafStage2NConc
    {
        get
        {
            double[] result = new double[mySpecies.Length];
            for (int s = 0; s < NumSpecies; s++)
                result[s] = mySpecies[s].leaves.tissue[1].Nconc;
            return result;
        }
    }

    /// <summary>N concentration in mature tissues of plant's leaves</summary>
    [Output]
    [Description("N concentration in mature tissues of plant's leaves")]
    [Units("kgN/kgDM")]
    public double[] SpeciesLeafStage3NConc
    {
        get
        {
            double[] result = new double[mySpecies.Length];
            for (int s = 0; s < NumSpecies; s++)
                result[s] = mySpecies[s].leaves.tissue[2].Nconc;
            return result;
        }
    }

    /// <summary>N concentration in dead tissues of plant's leaves</summary>
    [Output]
    [Description("N concentration in dead tissues of plant's leaves")]
    [Units("kgN/kgDM")]
    public double[] SpeciesLeafStage4NConc
    {
        get
        {
            double[] result = new double[mySpecies.Length];
            for (int s = 0; s < NumSpecies; s++)
                result[s] = mySpecies[s].leaves.tissue[3].Nconc;
            return result;
        }
    }

    /// <summary>N concentration in emerging tissues of plant's stems</summary>
    [Output]
    [Description("N concentration in emerging tissues of plant's stems")]
    [Units("kgN/kgDM")]
    public double[] SpeciesStemStage1NConc
    {
        get
        {
            double[] result = new double[mySpecies.Length];
            for (int s = 0; s < NumSpecies; s++)
                result[s] = mySpecies[s].stems.tissue[0].Nconc;
            return result;
        }
    }

    /// <summary>N concentration in developing tissues of plant's stems</summary>
    [Output]
    [Description("N concentration in developing tissues of plant's stems")]
    [Units("kgN/kgDM")]
    public double[] SpeciesStemStage2NConc
    {
        get
        {
            double[] result = new double[mySpecies.Length];
            for (int s = 0; s < NumSpecies; s++)
                result[s] = mySpecies[s].stems.tissue[1].Nconc;
            return result;
        }
    }

    /// <summary>N concentration in mature tissues of plant's stems</summary>
    [Output]
    [Description("N concentration in mature tissues of plant's stems")]
    [Units("kgN/kgDM")]
    public double[] SpeciesStemStage3NConc
    {
        get
        {
            double[] result = new double[mySpecies.Length];
            for (int s = 0; s < NumSpecies; s++)
                result[s] = mySpecies[s].stems.tissue[2].Nconc;
            return result;
        }
    }

    /// <summary>N concentration in dead tissues of plant's stems</summary>
    [Output]
    [Description("N concentration in dead tissues of plant's stems")]
    [Units("kgN/kgDM")]
    public double[] SpeciesStemStage4NConc
    {
        get
        {
            double[] result = new double[mySpecies.Length];
            for (int s = 0; s < NumSpecies; s++)
                result[s] = mySpecies[s].stems.tissue[3].Nconc;
            return result;
        }
    }

    /// <summary>N concentration in emerging tissues of plant's stolons</summary>
    [Output]
    [Description("N concentration in emerging tissues of plant's stolons")]
    [Units("kgN/kgDM")]
    public double[] SpeciesStolonStage1NConc
    {
        get
        {
            double[] result = new double[mySpecies.Length];
            for (int s = 0; s < NumSpecies; s++)
                result[s] = mySpecies[s].stolons.tissue[0].Nconc;
            return result;
        }
    }

    /// <summary>N concentration in developing tissues of plant's stolons</summary>
    [Output]
    [Description("N concentration in developing tissues of plant's stolons")]
    [Units("kgN/kgDM")]
    public double[] SpeciesStolonStage2NConc
    {
        get
        {
            double[] result = new double[mySpecies.Length];
            for (int s = 0; s < NumSpecies; s++)
                result[s] = mySpecies[s].stolons.tissue[1].Nconc;
            return result;
        }
    }

    /// <summary>N concentration in mature tissues of plant's stolons</summary>
    [Output]
    [Description("N concentration in mature tissues of plant's stolons")]
    [Units("kgN/kgDM")]
    public double[] SpeciesStolonStage3NConc
    {
        get
        {
            double[] result = new double[mySpecies.Length];
            for (int s = 0; s < NumSpecies; s++)
                result[s] = mySpecies[s].stolons.tissue[2].Nconc;
            return result;
        }
    }

    #endregion  - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -

    #endregion  ------------------------------------------------------------------------------------------------------------
}

//// =======================================================================================================================

/// <summary>Data and method for linear interpolation</summary>
public class LinearInterpolation
{
    /// <summary>
    /// The X and Y values, pairwise
    /// </summary>
    [Param] public string[] XYs;

    private double[] X;
    private double[] Y;

    /// <summary>
    /// Initialise the function
    /// </summary>
    [EventHandler]
    public void OnInitialised()
    {
        X = new double[XYs.Length];
        Y = new double[XYs.Length];
        for (int i = 0; i < XYs.Length; i++)
        {
            string[] XYBits = XYs[i].Split(",".ToCharArray(), StringSplitOptions.RemoveEmptyEntries);
            if (XYBits.Length != 2)
                throw new Exception("Invalid XY coordinate for function. Value: " + XYs[i]);
            X[i] = Convert.ToDouble(XYBits[0]);
            Y[i] = Convert.ToDouble(XYBits[1]);
        }
    }

    /// <summary>
    /// Interpolation
    /// </summary>
    /// <param name="dX">X value</param>
    /// <returns>Y value</returns>
    public double Value(double dX)
    {
        bool DidInterpolate = false;
        return MathUtility.LinearInterpReal(dX, X, Y, out DidInterpolate);
    }
}
