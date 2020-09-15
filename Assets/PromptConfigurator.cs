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

    public GameObject continueCentered;
    public GameObject continueSide;
    public GameObject cancelSide;

    public Queue<PromptRequest> requests = new Queue<PromptRequest>();

    PromptRequest promptRequest;

    void Awake () {
        inst = this;
    }

    #region QueuePromptText
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

    public static void QueuePromptText (string title, string text, Action continueCallback, Action cancelCallback) {
        if(inst == null) {
            return;
        }

        inst.requests.Enqueue(new PromptRequest(title, text, continueCallback, cancelCallback));

        if(!inst.display.gameObject.activeSelf) {
            inst.display.gameObject.SetActive(true);
            inst.OnContinue();
        }
    }
    #endregion

    public void OnContinue () {
        if(promptRequest != null)
            promptRequest.continueCallback?.Invoke();

        if(requests.Count > 0) {
            promptRequest = requests.Dequeue();
            DisplayRequest(promptRequest);
        } else {
            promptRequest = null;
            display.gameObject.SetActive(false);
        }
    }

    public void OnCancel () {
        if(promptRequest != null)
            promptRequest.cancelCallback?.Invoke();

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

        if(promptRequest.useCancelCallback) {
            continueCentered.SetActive(false);
            continueSide.SetActive(true);
            cancelSide.SetActive(true);
        } else {
            continueCentered.SetActive(true);
            continueSide.SetActive(false);
            cancelSide.SetActive(false);
        }
    }
}

public class PromptRequest {
    public string title;
    public string text;
    public Action continueCallback;
    public Action cancelCallback;
    public bool useCancelCallback;

    public PromptRequest (string title, string text) {
        this.title = title;
        this.text = text;
        continueCallback = null;
        cancelCallback = null;
        useCancelCallback = false;
    }

    public PromptRequest (string title, string text, Action callback) : this(title, text) {
        continueCallback = callback;
        useCancelCallback = false;
    }

    public PromptRequest (string title, string text, Action continueCallback, Action cancelCallback) : this(title, text) {
        this.continueCallback = continueCallback;
        this.cancelCallback = cancelCallback;
        useCancelCallback = true;
    }
}