 using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class Keytarget : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler {

	Text text;
	Image image;
	InputInterface.KeyBinding keyBinding;
	bool listening;
    Color goalColor;

	public void Awake() {
		text = this.GetComponentInChildren<Text>();
		image = this.GetComponent<Image>();
        goalColor = new Color(.8f, .8f, .8f);
        image.color = goalColor;
		listening = false;
	}

	private void Update() {
		if(listening) {
			foreach(KeyCode key in System.Enum.GetValues(typeof(KeyCode))) {
				if(Input.GetKeyDown(key))
					SetBinding(new InputInterface.KeyBinding(keyBinding.name, key), false);
			}
		}

        float r = UFUtils.LerpExpFactor(0.1f, Time.unscaledDeltaTime);
        image.color = Color.Lerp(image.color, goalColor, r);
	}

	public void SetBinding(InputInterface.KeyBinding binding, bool initialSet) {
        if(initialSet)
            Awake();
		this.keyBinding = binding;
        string bindingName = UFUtils.Capitalize(binding.name);
		this.text.text = bindingName + " : " + InputInterface.GetKeyName(binding.code);
		Global.input.SetBinding(binding, initialSet);
	}

    public void OnPointerEnter(PointerEventData eventData) {
        goalColor = new Color(1f, 1f, 1f);
        listening = true;
    }

    public void OnPointerExit(PointerEventData eventData) {
        goalColor = new Color(.8f, .8f, .8f);
        listening = false;
    }
}