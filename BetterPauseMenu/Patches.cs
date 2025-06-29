using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System;

namespace BetterPauseMenu
{
    [HarmonyPatch(typeof(PauseMenu), nameof(PauseMenu.Pause))]
    internal class PauseMenuPatch
    {
        internal static bool IsInConfirmation = false; // Track confirmation state

        static bool Prefix()
        {
            if (IsInConfirmation)
            {
                Plugin.Log.LogInfo("[BetterPauseMenu] Skipping PauseMenu.Pause() because confirmation is active.");
                return false; // Skip PauseMenu.Pause while in confirmation
            }
            return true; // Allow normal PauseMenu.Pause otherwise
        }

        static void Postfix(PauseMenu __instance)
        {
            var pauseMenuField = AccessTools.Field(typeof(PauseMenu), "pauseMenu");
            GameObject pauseMenu = pauseMenuField.GetValue(__instance) as GameObject;
            if (pauseMenu == null) return;

            if (pauseMenu.transform.Find("Restart") != null) return;

            Transform quitToMenuButton = pauseMenu.transform.Find("Quit to Menu");
            if (quitToMenuButton == null) return;

            RectTransform templateRect = quitToMenuButton.GetComponent<RectTransform>();

            // === Add Restart button with confirmation ===
            AddButton(pauseMenu, quitToMenuButton, templateRect, "Restart", -34f, () =>
            {
                ShowConfirmation(() =>
                {
                    Plugin.Log.LogInfo("[BetterPauseMenu] Restart confirmed. Restarting level.");
                    __instance.UnPause();
                    SceneManager.LoadScene("GameScene");
                });
            });

            // === Add Quit to Desktop button ===
            AddButton(pauseMenu, quitToMenuButton, templateRect, "Quit to Desktop", -68f, () =>
            {
                ShowConfirmation(() =>
                {
                    Plugin.Log.LogInfo("[BetterPauseMenu] Quit confirmed. Exiting game.");
                    __instance.UnPause();
                    Application.Quit();
                });
            });

            // === Extend PauseMenu panel to fit new buttons ===
            RectTransform panelRect = pauseMenu.GetComponent<RectTransform>();
            if (panelRect != null)
            {
                Vector2 size = panelRect.sizeDelta;
                size.y += 80f;
                panelRect.sizeDelta = size;
            }

            // === Move all PauseMenu children 30f up ===
            foreach (Transform child in pauseMenu.transform)
            {
                var rect = child.GetComponent<RectTransform>();
                if (rect != null)
                {
                    rect.anchoredPosition += new Vector2(0f, 30f);
                }
            }
        }

        private static void AddButton(GameObject pauseMenu, Transform templateButton, RectTransform templateRect, string name, float yOffset, Action onClick)
        {
            try
            {
                var newButtonObj = GameObject.Instantiate(templateButton.gameObject, pauseMenu.transform);
                newButtonObj.name = name;
                newButtonObj.transform.SetAsLastSibling();

                var newRect = newButtonObj.GetComponent<RectTransform>();
                CopyRectTransformLayout(newRect, templateRect);
                newRect.anchoredPosition = templateRect.anchoredPosition + new Vector2(0f, yOffset);

                ReplaceButtonComponent(newButtonObj, templateButton.GetComponent<Button>(), onClick, name);
                newButtonObj.SetActive(true);
            }
            catch (Exception ex)
            {
                Plugin.Log.LogError($"Error creating {name} button: {ex}");
            }
        }

        private static void ShowConfirmation(Action onConfirm)
        {
            var pm = PauseMenu.instance;
            if (pm == null)
            {
                Plugin.Log.LogWarning("[BetterPauseMenu] PauseMenu instance not found. Quitting immediately.");
                onConfirm.Invoke();
                return;
            }

            var areYouSureMenuField = AccessTools.Field(typeof(PauseMenu), "areYouSureMenu");
            GameObject areYouSureMenu = areYouSureMenuField.GetValue(pm) as GameObject;

            var pauseMenuField = AccessTools.Field(typeof(PauseMenu), "pauseMenu");
            GameObject pauseMenu = pauseMenuField.GetValue(pm) as GameObject;

            if (areYouSureMenu == null)
            {
                Plugin.Log.LogWarning("[BetterPauseMenu] Confirmation menu not found. Quitting immediately.");
                onConfirm.Invoke();
                return;
            }

            IsInConfirmation = true;
            areYouSureMenu.SetActive(true);
            if (pauseMenu != null) pauseMenu.SetActive(false);

            Plugin.Log.LogInfo("[BetterPauseMenu] Confirmation menu opened. Listing children to find buttons:");
            foreach (Transform child in areYouSureMenu.transform)
                Plugin.Log.LogInfo($"[BetterPauseMenu] areYouSureMenu child: {child.name}");

            var yesButtonTransform = areYouSureMenu.transform.Find("Yes");
            var noButtonTransform = areYouSureMenu.transform.Find("No");

            if (yesButtonTransform == null || noButtonTransform == null)
            {
                Plugin.Log.LogError("[BetterPauseMenu] Could not find Yes/No buttons. Quitting immediately as fallback.");
                areYouSureMenu.SetActive(false);
                if (pauseMenu != null) pauseMenu.SetActive(true);
                IsInConfirmation = false;
                onConfirm.Invoke();
                return;
            }

            var yesButton = yesButtonTransform.GetComponent<Button>();
            var noButton = noButtonTransform.GetComponent<Button>();

            yesButton.onClick.RemoveAllListeners();
            noButton.onClick.RemoveAllListeners();

            yesButton.onClick.AddListener(() =>
            {
                Plugin.Log.LogInfo("[BetterPauseMenu] Confirmation: YES clicked.");
                areYouSureMenu.SetActive(false);
                if (pauseMenu != null) pauseMenu.SetActive(true);
                IsInConfirmation = false;

                // Force-hide UI panels before quitting
                Plugin.Log.LogInfo("[BetterPauseMenu] Forcing hide of GameUI panels before quitting.");
                var gameOverObj = GameObject.Find("GameOver");
                var victoryObj = GameObject.Find("Victory");
                var damageTotalsObj = GameObject.Find("DamageTotals");

                if (gameOverObj != null) gameOverObj.SetActive(false);
                if (victoryObj != null) victoryObj.SetActive(false);
                if (damageTotalsObj != null) damageTotalsObj.SetActive(false);

                onConfirm.Invoke(); // This will trigger Application.Quit();
            });

            noButton.onClick.AddListener(() =>
            {
                Plugin.Log.LogInfo("[BetterPauseMenu] Confirmation: NO clicked.");
                areYouSureMenu.SetActive(false);
                if (pauseMenu != null) pauseMenu.SetActive(true);
                IsInConfirmation = false;
            });
        }

        private static void CopyRectTransformLayout(RectTransform target, RectTransform source)
        {
            target.anchorMin = source.anchorMin;
            target.anchorMax = source.anchorMax;
            target.pivot = source.pivot;
            target.sizeDelta = source.sizeDelta;
        }

        private static void ReplaceButtonComponent(GameObject buttonObj, Button templateButton, Action onClickAction, string buttonText)
        {
            var oldBtn = buttonObj.GetComponent<Button>();
            if (oldBtn != null) UnityEngine.Object.DestroyImmediate(oldBtn);

            var newBtn = buttonObj.AddComponent<Button>();
            CopyButtonVisuals(newBtn, templateButton);
            newBtn.onClick.AddListener(() => onClickAction.Invoke());

            var txt = buttonObj.GetComponentInChildren<Text>();
            if (txt != null)
            {
                txt.text = buttonText;
            }
        }

        private static void CopyButtonVisuals(Button target, Button source)
        {
            target.transition = source.transition;
            target.colors = source.colors;
            target.spriteState = source.spriteState;
            target.animationTriggers = source.animationTriggers;

            var img = target.GetComponent<Image>();
            var srcImg = source.GetComponent<Image>();
            if (img != null && srcImg != null)
            {
                img.sprite = srcImg.sprite;
                img.type = srcImg.type;
                img.color = srcImg.color;
                img.raycastTarget = true;
                target.targetGraphic = img;
            }
        }
    }
}
