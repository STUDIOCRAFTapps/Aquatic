using System;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class PromptConfigurator : MonoBehaviour {

    public static PromptConfigurator inst;
    public RectTransform display;

    public TextMeshProUGUI title;
    public TextMeshProUGUI titleShadow;
    public TextMeshProUGUI text;
    public TextMeshProUGUI textShadow;

    public Queue<PromptRequest> requests = new Queue<PromptRequest>();

    PromptRequest promptRequest;

    void Awake () {
        inst = this;
    }

    public static void QueuePromptText (string title, string text) {
        if(inst == null) {
            return;
        }

        inst.requests.Enqueue(new PromptRequest(title, text));

        if(!inst.display.gameObject.activeSelf) {
            inst.display.gameObject.SetActive(true);
            inst.OnContinue();
        }
    }

    public static void QueuePromptText (string title, string text, Action callback) {
        if(inst == null) {
            return;
        }

        inst.requests.Enqueue(new PromptRequest(title, text, callback));

        if(!inst.display.gameObject.activeSelf) {
            inst.display.gameObject.SetActive(true);
            inst.OnContinue();
        }
    }

    public void OnContinue () {
        if(promptRequest != null)
            promptRequest.callback?.Invoke();

        if(requests.Count > 0) {
            promptRequest = requests.Dequeue();
            DisplayRequest(promptRequest);
        } else {
            promptRequest = null;
            display.gameObject.SetActive(false);
        }
    }

    void DisplayRequest (PromptRequest promptRequest) {
        title.SetText(promptRequest.title);
        titleShadow.SetText(promptRequest.title);
        text.SetText(promptRequest.text);
        textShadow.SetText(promptRequest.text);
    }
}

public class PromptRequest {
    public string title;
    public string text;
    public Action callback;

    public PromptRequest (string title, string text) {
        this.title = title;
        this.text = text;
        callback = null;
    }

    public PromptRequest (string title, string text, Action callback) : this(title, text) {
        this.callback = callback;
    }
}