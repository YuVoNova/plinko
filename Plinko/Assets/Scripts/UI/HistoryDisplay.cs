using UnityEngine;
using Data;

namespace UI
{
    public class HistoryDisplay : MonoBehaviour
    {
        [SerializeField] private Transform contentParent;
        [SerializeField] private int maxEntries = 5;
        
        private int _activeCount;
        
        private void Awake()
        {
            HideAllEntries();
        }
        
        public void AddEntry(BallResultData data)
        {
            int lastIndex = contentParent.childCount - 1;
            Transform lastChild = contentParent.GetChild(lastIndex);
            
            if (_activeCount < maxEntries)
            {
                lastChild.gameObject.SetActive(true);
                _activeCount++;
            }
            
            lastChild.SetAsFirstSibling();
            lastChild.GetComponent<HistoryEntryUI>().SetData(data);
        }
        
        public void ClearHistory()
        {
            HideAllEntries();
            _activeCount = 0;
        }
        
        private void HideAllEntries()
        {
            foreach (Transform child in contentParent)
            {
                child.gameObject.SetActive(false);
            }
        }
    }
}