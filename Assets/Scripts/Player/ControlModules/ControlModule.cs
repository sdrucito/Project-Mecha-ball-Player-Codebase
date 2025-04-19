using UnityEngine;
/*
 * Superclass for a Control Module, used by the ControlModuleManager
 * to group all the available modules and decide which one is active
 */
public class ControlModule : MonoBehaviour
{

    protected string _name = "None";

    public string GetName()
    {
        return _name;
    }
}
