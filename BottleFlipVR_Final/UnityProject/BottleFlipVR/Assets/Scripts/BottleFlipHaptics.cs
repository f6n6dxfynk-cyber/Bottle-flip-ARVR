using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;

/// <summary>Best-effort XR controller vibration with a silent desktop fallback.</summary>
public static class BottleFlipHaptics
{
    public static void Pulse(float amplitude, float duration)
    {
        var devices = new List<InputDevice>();
        InputDevices.GetDevicesWithCharacteristics(InputDeviceCharacteristics.HeldInHand | InputDeviceCharacteristics.Controller, devices);
        foreach (InputDevice device in devices)
        {
            if (device.TryGetHapticCapabilities(out HapticCapabilities capabilities) && capabilities.supportsImpulse)
            {
                device.SendHapticImpulse(0u, amplitude, duration);
            }
        }
    }
}
