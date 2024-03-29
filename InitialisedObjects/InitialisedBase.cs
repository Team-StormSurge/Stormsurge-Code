﻿using System;
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

namespace StormSurge.InitialisedObjects
{
    public static class BaseInitialiser
    {
        

    }
    public abstract class InitialisedBase
    {
        //inherited properties n methods
        public abstract void InitFunction();
        protected PatchClassProcessor? PatchProcessor;

        //config methods
        protected abstract string name { get; }
        public ConfigEntry<bool>? Enabled;
        protected virtual bool AddConfig()
        {
            Enabled = Config.configFile!.Bind(name, "Enabled", true, $"Whether this is enabled.");
            return (Enabled.Value);
        }

        //static (one-time) properties n methods
        public static List<InitialisedBase> initialisedBases = new();
        public static Dictionary<Type, InitialisedBase> baseDict = new();
        public static T? GetInstance<T>() where T : InitialisedBase => baseDict[typeof(T)] as T;
        public static void InitialiseAll(Harmony harmony)
        {
            Type baseType = typeof(InitialisedBase);
            foreach (Type initType in Assembly.GetCallingAssembly().GetTypes().Where(t => t.IsAssignableFrom(baseType) && !t.IsAbstract))
            {
                InitialisedBase? baseInstance = Activator.CreateInstance(initType) as InitialisedBase;
                if (harmony != null && baseInstance != null)
                {
                    baseInstance.PatchProcessor = new PatchClassProcessor(harmony, baseInstance.GetType());
                    baseInstance.PatchProcessor.Patch();
                }
                initialisedBases.Add(baseInstance!);
                baseDict.Add(initType, baseInstance!);
                UnityEngine.Debug.LogWarning($"Initialising base {baseInstance}");

            }
        }
    }

}
