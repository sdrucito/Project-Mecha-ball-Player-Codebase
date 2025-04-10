using System;
using System.Collections.Generic;
using UnityEngine;

public class ControlModuleManager : MonoBehaviour
{
    [SerializeField] ControlModule ballModule;
    [SerializeField] ControlModule movementModule;

    private int _actualModule = 0;
    private List<ControlModule> _modules = new List<ControlModule>();
    private void Start()
    {
        _modules.Add(movementModule);
        _modules.Add(ballModule);
        _actualModule = 0;
        Mock_InputController.OnModeChangeInput += SwitchMode;
    }

    private void SwitchMode()
    {
        DeactivateModule();
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
    
    private void DeactivateModule()
    {
        _modules[_actualModule].enabled = false;
    }
}
