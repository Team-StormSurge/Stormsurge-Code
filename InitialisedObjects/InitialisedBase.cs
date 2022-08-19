using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using RoR2;
using RoR2.ContentManagement;

namespace StormSurge
{
    /// <summary>
    /// The grand-daddy of all of our modded behaviours, determining that it can be patched, initialised, etc.
    /// </summary>
    //declares that this class (and its children) can be used to patch the game
    [HarmonyPatch]
    public abstract class InitialisedBase
    {
        //inherited properties n methods
        //the constructor for this method; runs an abstract method that child classes may inherit
        public InitialisedBase()
        {
            InitFunction();
        }

        //inherited by child classes; may do nothing, or initialise specific behaviours
        /// <summary>
        /// Function to initialise any specific behaviours for this modded class.
        /// </summary>
        public abstract void InitFunction();
        //the processor that patches the game for us
        protected PatchClassProcessor? PatchProcessor;

        //config methods
        /// <summary>
        /// The name of this behaviour's Config Category in BepinEx
        /// </summary>
        protected abstract string configName { get; }
        //true for all configurable objects; whether this component is enabled- is true by default
        public ConfigEntry<bool>? Enabled;
        /// <summary>
        /// Adds all config entries for this item, and determines whether this item is enabled
        /// </summary>
        /// <returns>whether this item is enabled/disabled in config</returns>
        protected virtual bool AddConfig()
        {
            Enabled = Config.configFile!.Bind(configName, "Enabled", true, $"Whether this is enabled.");
            return (Enabled.Value);
        }

        //static (one-time) properties n methods
        //a list of all Initialised mod behaviours we've found, and a dictionary to find them by Type
        public static List<InitialisedBase> initialisedBases = new();
        public static Dictionary<Type, InitialisedBase> baseDict = new();

        //returns the instance of any given modded behaviour type
        public static T? GetInstance<T>() where T : InitialisedBase => baseDict[typeof(T)] as T;

        //runs at game load; initialises all of our modded behaviours
        public static void InitialiseAll(Harmony harmony)
        {
            //finds all classes in our mod that are InitialisedBase children, and are not abstract definitions
            Type baseType = typeof(InitialisedBase);
            foreach (Type initType in Assembly.GetCallingAssembly().GetTypes().Where(t => baseType.IsAssignableFrom(t) && !t.IsAbstract))
            {
                //initialise our behaviour, and add it to our instance list
                InitialisedBase? baseInstance = Activator.CreateInstance(initType) as InitialisedBase;
                initialisedBases.Add(baseInstance!);
                baseDict.Add(initType, baseInstance!);
                //if the behaviour has a patch processor, we patch it in here
                if (harmony != null && baseInstance != null)
                {
                    baseInstance.PatchProcessor = new PatchClassProcessor(harmony, baseInstance.GetType());
                    baseInstance.PatchProcessor.Patch();
                }

            }
        }
    }

}
