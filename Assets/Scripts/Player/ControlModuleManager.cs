using System;
using System.Collections.Generic;
using UnityEngine;

/*
 * Manages every control module attached to the player. Supervise the switch between different modules
 */
public class ControlModuleManager : MonoBehaviour
{
    
    [SerializeField] private int _actualModule = 0; // Index of the active module //TODO Make this private
    [SerializeField] private List<ControlModule> _modules = new List<ControlModule>();  // List of all available control modules
    private void Start()
    {
        GetAvailableControlModules();
        _actualModule = 0;
        ActivateModule();
        DeactivateOtherModules();
        PlayerInputManager.Instance.OnModeChangeInput += SwitchMode;
    }

    // Search for modules in sub-objects and insert them into a list
    // Every time a new module has to be added, it is simply created with an empty sub-object of the control module manager
    private void GetAvailableControlModules()
    {
        for (var i = 0; i < gameObject.transform.childCount; i++)
        {
            var childModule = gameObject.transform.GetChild(i).GetComponent<ControlModule>();
            if (childModule)
            {
                _modules.Add(childModule);
            }
        }
    }
    private void SwitchMode()
    {
        Debug.Log("SwitchMode");
        _actualModule = GetNextModule();
        ActivateModule();
    }

    private int GetNextModule()
    {
        int nextModuleIndex = _actualModule + 1;
        if (nextModuleIndex >= _modules.Count)
        {
            nextModuleIndex = 0;
        }
        return nextModuleIndex;
    }

    /*
     * Methods that activate/deactivate a control module
     * Everytime the enabled is changed it triggers the OnEnable/OnDisable function in the module itself
     */
    private void ActivateModule()
    {
        _modules[_actualModule].enabled = true;
    }
    
    private void DeactivateOtherModules()
    {
        int i = 0;
        foreach (ControlModule module in _modules)
            if (_modules.IndexOf(module) != _actualModule)
                module.enabled = false;
    }

    private void Update()
    {
        Debug.Log("Current module:"+_modules[_actualModule].name);
    }
}
