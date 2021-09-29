using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Unity.Mathematics;

public class NumberedSlider : MonoBehaviour {

    public TextMeshProUGUI number;
    public TMP_InputField inputField;
    public Slider slider;
    public string suffix;
    public float roundTo = 1f;
    public bool useReverseScaleRemap;
    public float reverseMin;
    public float reverseMax;

    private void Start () {
        slider.GetComponent<Slider>().onValueChanged.AddListener(OnValueChangedEvent);
        inputField?.onEndEdit.AddListener(OnInputFieldChanged);
    }

    public float value {
        get {
            if(useReverseScaleRemap) {
                return math.round(Range11ToValue(slider.value) * roundTo) / roundTo;
            } else {
                return math.round(slider.value * roundTo) / roundTo;
            }
        }
        set {
            if(useReverseScaleRemap) {
                float roundedInput = math.round(value * roundTo) / roundTo;
                slider.value = ValueToRange(roundedInput);
                DisplayValue(roundedInput);
            } else {
                float roundedInput = math.round(value * roundTo) / roundTo;
                slider.value = roundedInput;
                DisplayValue(roundedInput);
            }
        }
    }

    private void OnValueChangedEvent (float value) {
        if(useReverseScaleRemap) {
            this.value = Range11ToValue(value);
        } else {
            this.value = value;
        }
    }
    
    private void DisplayValue (float rawValue) {
        if(number != null) {
            number.text = $"{rawValue}{suffix}";
        }
        if(inputField != null) {
            inputField.text = $"{rawValue}{suffix}";
        }
    }

    private void OnInputFieldChanged (string value) {
        value = value.Replace(suffix, string.Empty);
        if(float.TryParse(value, out float result)) {
            slider.value = result;
        } else {
            slider.value = (slider.minValue + slider.maxValue) * 0.5f;
        }
        inputField.text = $"{math.round(result * roundTo) / roundTo}{suffix}";
    }

    private float Range11ToValue (float range) {
        return math.select(
            math.lerp(1, reverseMin, -range),
            math.lerp(1, reverseMax, range),
            range > 0
        );
    }

    private float ValueToRange (float value) {
        return math.select(
            -math.unlerp(1, reverseMin, value),
            math.unlerp(1, reverseMax, value),
            value > 1
        );
    }
}
