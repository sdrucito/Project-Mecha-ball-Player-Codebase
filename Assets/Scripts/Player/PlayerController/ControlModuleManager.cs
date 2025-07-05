using System;
using System.Collections.Generic;
using Player.Animation;
using Player.ControlModules;
using UnityEngine;
using UnityEngine.Events;

/*
 * Manages every control module attached to the player. Supervise the switch between different modules
 */
namespace Player.PlayerController
{
    public class ControlModuleManager : MonoBehaviour
    {
    
        private int _actualModule = 0; // Index of the active module
        private int _previousModule = 0; // Index of the previous module for rollback during switches
        private List<ControlModule> _modules = new List<ControlModule>();  // List of all available control modules
        public bool IsSwitching { get; private set; }
        public string GetActiveModuleName()
        {
            return _modules[_actualModule].name;
        }

        public ControlModule GetModule(string moduleName)
        {
            return _modules.Find(module => module.name == moduleName);
        }
        private void Awake()
        {
            GetAvailableControlModules();
            _actualModule = 0;
            _previousModule = 0;

            if (GetActiveModuleName() == "Ball")
            {
                _actualModule = 1;
                _previousModule = 1;
            }
        }

        private void Start()
        {
            
            DeactivateAllModules();
            ActivateModule();
        
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
        public void SwitchMode()
        {
            // Switch only if it's grounded
            if (CanSwitch())
            {
                PlayerInputManager.Instance.SetActionEnabled("ChangeMode", false);
                _actualModule = GetNextModule();
                DeactivateAllModules();
                IsSwitching = true;
                _modules[_actualModule].OnActivated?.Invoke();
                HapticsManager.Instance.Play("SwitchMode");
                AudioManager.Instance.PlayOneShot(FMODEvents.Instance.robotSwitchInput.eventReference, transform.position);
            }
        }

        private bool CanSwitch()
        {
            Player player = Player.Instance;
            return player.IsGrounded() && player.PlayerState == PlayerState.Unoccupied && !IsSwitching;
        }
        

        public void ActivateNextModule()
        {
            ActivateModule();
        }

        public void RollbackSwitch()
        {
            _actualModule = _previousModule;
            DeactivateAllModules();
            IsSwitching = true;
            _modules[_actualModule].OnActivated?.Invoke();
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
            IsSwitching = false;
            _previousModule = _actualModule;
            //Debug.Log("Enabled Module: " + GetActiveModuleName());
        }
    
        private void DeactivateAllModules()
        {
            foreach (ControlModule module in _modules)
                module.enabled = false;
        }

        private void OnDestroy()
        {
            if (PlayerInputManager.Instance != null)
            {
                PlayerInputManager.Instance.OnModeChangeInput -= SwitchMode;
            }
        }
    }
}
