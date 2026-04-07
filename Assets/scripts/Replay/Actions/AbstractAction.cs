using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public abstract class AbstractAction
{
    public static Dictionary<string, Type> actionTypes;
    public static string type;
    public abstract string Serialize();

    public Dictionary<string, Type> GetTypes()
    {
        // if (actionTypes == null || actionTypes.Count == 0) 
        // {
        //     actionTypes = typeof(AbstractAction)
        //     .Assembly.GetTypes()
        //     .Where(type => type.IsSubclassOf(typeof(AbstractAction)) && !type.IsAbstract)
        //     .ToDictionary(t => t.GetField("type", System.Reflection.BindingFlags.Static).GetValue(), t => t);
        // }
        return actionTypes;
    }
}