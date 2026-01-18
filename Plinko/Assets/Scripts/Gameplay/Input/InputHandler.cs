using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace Gameplay
{
    public class InputHandler : MonoBehaviour
    {
        private bool _isPressed;
        
        public Action OnSpawnPressed;
        public Action OnSpawnReleased;
        
        private void Update()
        {
            HandleInput();
        }
        
        public void ForceRelease()
        {
            if (!_isPressed)
                return;
            
            _isPressed = false;
            OnSpawnReleased?.Invoke();
        }
        
        private void HandleInput()
        {
            bool currentlyPressed = false;
            
            if (Mouse.current != null && Mouse.current.leftButton.isPressed)
            {
                Vector2 pos = Mouse.current.position.ReadValue();
                if (!IsPointerOverUI(pos))
                    currentlyPressed = true;
            }
            else if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.isPressed)
            {
                Vector2 pos = Touchscreen.current.primaryTouch.position.ReadValue();
                if (!IsPointerOverUI(pos))
                    currentlyPressed = true;
            }
            
            if (currentlyPressed && !_isPressed)
            {
                _isPressed = true;
                OnSpawnPressed?.Invoke();
            }
            else if (!currentlyPressed && _isPressed)
            {
                _isPressed = false;
                OnSpawnReleased?.Invoke();
            }
        }
        
        private bool IsPointerOverUI(Vector2 position)
        {
            if (!EventSystem.current)
                return false;
            
            PointerEventData eventData = new PointerEventData(EventSystem.current)
            {
                position = position
            };
            
            List<RaycastResult> results = new System.Collections.Generic.List<RaycastResult>();
            EventSystem.current.RaycastAll(eventData, results);
            
            return results.Count > 0;
        }
    }
}