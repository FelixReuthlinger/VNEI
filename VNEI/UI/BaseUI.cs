﻿using System;
using System.Collections.Generic;
using System.Linq;
using Jotunn.GUI;
using UnityEngine;
using Jotunn.Managers;
using UnityEngine.UI;
using VNEI.Logic;

namespace VNEI.UI {
    [DefaultExecutionOrder(5)]
    public class BaseUI : MonoBehaviour {
        [Header("Local References")] public RectTransform root;
        public RectTransform dragHandler;
        public Transform lastViewItemsParent;

        public SearchUI searchUi;
        public RecipeUI recipeUi;

        [Header("Prefabs")] public GameObject itemPrefab;
        public GameObject rowPrefab;
        public GameObject recipeDroppedTextPrefab;
        public GameObject arrowPrefab;

        private List<DisplayItem> lastViewedDisplayItems = new List<DisplayItem>();
        private List<Item> lastViewedItems = new List<Item>();
        private bool blockInput;
        private bool sizeDirty;
        public List<TypeToggle> typeToggles = new List<TypeToggle>();

        public static void Create() {
            GameObject prefab = Plugin.AssetBundle.LoadAsset<GameObject>("VNEI");
            Instantiate(prefab, GUIManager.CustomGUIFront.transform);
        }

        private void Awake() {
            searchUi.baseUI = this;
            recipeUi.baseUI = this;

            dragHandler.gameObject.AddComponent<DragWindowCntrl>();
            ShowSearch();

            Styling.ApplyAllComponents(root);
            GUIManager.Instance.ApplyWoodpanelStyle(dragHandler);

            if ((bool) InventoryGui.instance) {
                transform.SetParent(InventoryGui.instance.m_player);
                ((RectTransform) transform).anchoredPosition = new Vector2(665, -45);
            } else {
                root.gameObject.SetActive(false);
                dragHandler.gameObject.SetActive(false);
            }

            recipeUi.OnSetItem += AddItemToLastViewedQueue;
            Plugin.columnCount.SettingChanged += RebuildSizeEvent;
            Plugin.rowCount.SettingChanged += RebuildSizeEvent;

            RebuildSize();
            RebuildLastViewedDisplayItems();
        }

        private void RebuildSizeEvent(object sender, EventArgs e) => sizeDirty = true;

        private void RebuildSize() {
            root.sizeDelta = new Vector2(Plugin.columnCount.Value * 50f + 20f,
                Plugin.rowCount.Value * 50f + 110f);
            dragHandler.sizeDelta = root.sizeDelta + new Vector2(10f, 10f);
        }

        private void Update() {
            if (searchUi.searchField.isFocused && !blockInput) {
                GUIManager.BlockInput(true);
                blockInput = true;
            } else if (!searchUi.searchField.isFocused && blockInput) {
                GUIManager.BlockInput(false);
                blockInput = false;
            }

            if (sizeDirty) {
                sizeDirty = false;
                RebuildSize();
                RebuildLastViewedDisplayItems();
            }
        }

        private void RebuildLastViewedDisplayItems() {
            foreach (DisplayItem displayItem in lastViewedDisplayItems) {
                Destroy(displayItem.gameObject);
            }

            lastViewedDisplayItems.Clear();

            int lastViewCount = Mathf.Max(Plugin.columnCount.Value - 5, 0);

            RectTransform parentRectTransform = ((RectTransform) lastViewItemsParent.transform);
            parentRectTransform.anchoredPosition = new Vector2((lastViewCount * 50f) / 2f + 5f, -25f);
            parentRectTransform.sizeDelta = new Vector2(lastViewCount * 50f, 50f);

            for (int i = 0; i < lastViewCount; i++) {
                GameObject sprite = Instantiate(itemPrefab, lastViewItemsParent);
                ((RectTransform) sprite.transform).anchoredPosition = new Vector2(25f + i * 50, -25f);
                DisplayItem displayItem = sprite.GetComponent<DisplayItem>();
                displayItem.Init(this);
                lastViewedDisplayItems.Add(displayItem);
            }

            UpdateLastViewDisplayItems();
        }

        private void LateUpdate() {
            root.anchoredPosition = dragHandler.anchoredPosition;
        }

        private void HideAll() {
            searchUi.gameObject.SetActive(false);
            recipeUi.gameObject.SetActive(false);
        }

        public void ShowSearch() {
            HideAll();
            searchUi.gameObject.SetActive(true);
        }

        public void ShowRecipe() {
            HideAll();
            recipeUi.gameObject.SetActive(true);
        }

        private void AddItemToLastViewedQueue(Item item) {
            // add new item at start
            lastViewedItems.Insert(0, item);

            // remove duplicate items
            for (int i = 1; i < lastViewedItems.Count; i++) {
                if (lastViewedItems[i] == item) {
                    lastViewedItems.RemoveAt(i);
                }
            }

            // remove overflowing items
            if (lastViewedItems.Count > 25) {
                lastViewedItems.RemoveAt(lastViewedItems.Count - 1);
            }

            // display items at corresponding slots
            UpdateLastViewDisplayItems();
        }

        private void UpdateLastViewDisplayItems() {
            for (int i = 0; i < lastViewedDisplayItems.Count; i++) {
                if (i >= lastViewedItems.Count) {
                    lastViewedDisplayItems[i].SetItem(null, 1);
                    continue;
                }

                lastViewedDisplayItems[i].SetItem(lastViewedItems[i], 1);
            }
        }

        private void OnDestroy() {
            recipeUi.OnSetItem -= AddItemToLastViewedQueue;
        }
    }
}
