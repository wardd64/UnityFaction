using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenu : MonoBehaviour {

    public Transform mapListContent;
    public MapListElement mleTemplate;
    public GameObject singlePlayerMenu, multiplayerMenu;

    public string mapDataFile = "MapData";
    private static CSVReader mapListData;
    public static CSVReader mapList{ get {
            if(mapListData == null) {
                TextAsset mapData = (TextAsset)Resources.Load("MapData", typeof(TextAsset));
                mapListData = new CSVReader(mapData);
            }
            return mapListData;
    } }


    public void Quit() {
        Application.Quit();
    }

    private void Start() {
        Global.hud.gameObject.SetActive(false);
        Global.igMenu.gameObject.SetActive(true);
        UFUtils.SetFPSCursor(false);
        MakeMapList();

        singlePlayerMenu.SetActive(false);
    }

    public void OpenOptions() {
        Global.igMenu.OpenOptions();
    }

    private void MakeMapList() {
        string[] maps = new string[mapList.nRow];
        for(int i = 0; i < mapList.nRow; i++)
            maps[i] = mapList.GetValue(i, "Scene name");
        Global.save.InitializeRecords(maps);

        for(int i = 0; i < mapList.nRow; i++) {
            MapListElement el = Instantiate(mleTemplate, mapListContent);

            string scene = mapList.GetValue(i, "Scene name");
            string title = mapList.GetValue(i, "Title");
            string author = mapList.GetValue(i, "Main author");
            string credits = mapList.GetValue(i, "Credits");
            string info = mapList.GetValue(i, "Additional info");
            float rating = 0f;
            float.TryParse(mapList.GetValue(i, "Rating"), out rating);
            float difficulty = 0f;
            float.TryParse(mapList.GetValue(i, "Difficulty"), out difficulty);
            el.SetMap(scene, title, author, credits, info, rating, difficulty);

            string previewName = mapList.GetValue(i, "Preview image");
            string previewPath = "MapPreviews/" + previewName;
            Sprite preview = Resources.Load<Sprite>(previewPath);
            el.previewImage.sprite = preview;

            bool foundMap = Global.levelLauncher.SceneIsAvailable(scene);

            string valid = mapList.GetValue(i, "Validated").ToLower();
            el.SetStatus(foundMap, valid);

            if(preview == null)
                Debug.LogWarning("Map preview could not be found: " + previewName);
            if(!foundMap)
                Debug.LogWarning("Scene not added to build settings: " + scene);

            Global.save.SetRecordText(scene, el.recordText);
        }

        Destroy(mapListContent.GetChild(0).gameObject);
    }

    public enum MapSortCriterium {
        Title = 1, Author = 2, Record = 3, Rating = 4, Difficulty = 5
    }

    public void SortMapList(int criterium) {
        MapSortCriterium nextCriterium = (MapSortCriterium)criterium;
        lastReverse ^= nextCriterium == lastCriterium;
        SortMapList(nextCriterium, lastReverse);
        lastCriterium = nextCriterium;
    }

    MapSortCriterium lastCriterium;
    bool lastReverse;

    public void SortMapList(MapSortCriterium criterium, bool reverse) {
        List<MapListElement> maps = new List<MapListElement>();
        for(int i = 0; i < mapListContent.childCount; i++) 
            maps.Add(mapListContent.GetChild(i).GetComponent<MapListElement>());

        IEnumerable<MapListElement> sort;

        switch(criterium) {
        case MapSortCriterium.Title:
        sort = Enumerable.OrderBy(maps, (el) => el.mapTitleText.text, 
            new AlphabetComparer(reverse));
        break;

        case MapSortCriterium.Author:
        sort = Enumerable.OrderBy(maps, (el) => el.mapAuthorText.text, 
            new AlphabetComparer(reverse));
        break;

        case MapSortCriterium.Record:
        sort = Enumerable.OrderBy(maps, (el) => el.recordText.text, 
            new RecordComparer(reverse));
        break;

        case MapSortCriterium.Rating:
        sort = Enumerable.OrderBy(maps, (el) => el.ratingImage.fillAmount, 
            new FloatComparer(reverse));
        break;

        case MapSortCriterium.Difficulty:
        sort = Enumerable.OrderBy(maps, (el) => el.difficultyImage.fillAmount, 
            new FloatComparer(reverse));
        break;

        default:
        Debug.LogError("Unkown map sort criterium: " + criterium);
        sort = null;
        break;
        }

        int eli = 0;
        foreach(MapListElement el in sort) {
            for(int j = eli; j < maps.Count; j++) {
                if(el == mapListContent.GetChild(j).GetComponent<MapListElement>()) {
                    mapListContent.GetChild(j).SetSiblingIndex(eli);
                    break;
                }
            }
            eli++;
        }
    }

    private class FloatComparer : IComparer<float> {

        bool reverse;

        public FloatComparer(bool reverse) {
            this.reverse = reverse;
        }

        public int Compare(float s1, float s2) {
            if(reverse)
                return s2.CompareTo(s1);
            return s1.CompareTo(s2);
        }
    }

    private class AlphabetComparer : IComparer<string> {

        bool reverse;

        public AlphabetComparer(bool reverse) {
            this.reverse = reverse;
        }

        public int Compare(string s1, string s2) {
            if(reverse)
                return s2.CompareTo(s1);
            return s1.CompareTo(s2);
        }
    }

    private class RecordComparer : IComparer<string> {

        bool reverse;

        public RecordComparer(bool reverse) {
            this.reverse = reverse;
        }

        public int Compare(string rec1, string rec2) {
            int toReturn = GetDifficulty(rec2) - GetDifficulty(rec1);
            return reverse ? -toReturn : toReturn;
        }

        private static int GetDifficulty(string rec) {
            rec = rec.ToLower();
            if(rec.Contains("casual"))
                return 0;
            if(rec.Contains("standard"))
                return 1;
            if(rec.Contains("brutal"))
                return 2;
            return -1;
        }
    }    
}
