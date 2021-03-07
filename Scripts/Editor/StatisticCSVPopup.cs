using System.Linq;
using System.IO;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using PlayRecorder.Tools;

namespace PlayRecorder.Stats
{
    public class StatisticCSVPopup : PopupWindowContent
    {
        private bool _isFinal = false;
        private float _width = 0f;
        private List<StatisticWindow.StatCache> _statsCache;

        private List<int> _fileIndexes = new List<int>();
        private List<string> _columns = new List<string>();
        private string _columnsRaw = "";

        private string _exampleText = "",_exampleTextNumber="",_exampleTextName="";

        private bool _keepFileNumbers = true, _keepFileNames = true;

        private int _totalRows = 0;

        private const string _keepNumbersKey = "PlayRecorder_CSV_Numbers", _keepNamesKey = "PlayRecorder_CSV_Names";

        public StatisticCSVPopup(List<StatisticWindow.StatCache> statsCache, bool final, float width)
        {
            _statsCache = statsCache;
            _isFinal = final;
            _width = width;
            for (int i = 0; i < _statsCache.Count; i++)
            {
                if (!_columns.Contains(_statsCache[i].statName))
                    _columns.Add(_statsCache[i].statName);

                if (!_fileIndexes.Contains(_statsCache[i].fileIndex))
                {
                    int f = _statsCache[i].fileIndex;
                    _fileIndexes.Add(f);
                }
            }
            _totalRows = _fileIndexes.Count;
            int tempInd = _statsCache[0].fileIndex;
            List<StatisticWindow.StatCache> tempCache = _statsCache.Where(x => x.fileIndex == tempInd).ToList();
            for (int i = 0; i < _columns.Count; i++)
            {
                _columnsRaw += _columns[i].ToCSVCell() + ",";
            }
            _columnsRaw = _columnsRaw.TrimEnd(',');
            _columnsRaw += "\n";
            _exampleTextNumber = _statsCache[0].fileIndex.ToString() + ',';
            _exampleTextName = _statsCache[0].fileName.ToCSVCell() + ',';
            _exampleText = GetCSVLine(tempInd, false, false).TrimEnd();

            if(EditorPrefs.HasKey(_keepNumbersKey))
            {
                _keepFileNumbers = EditorPrefs.GetBool(_keepNumbersKey);
            }
            if(EditorPrefs.HasKey(_keepNamesKey))
            {
                _keepFileNames = EditorPrefs.GetBool(_keepNamesKey);
            }
        }

        public override Vector2 GetWindowSize()
        {
            return new Vector2(_width-(Sizes.padding*3), ((Sizes.heightLine+Sizes.padding) * 7)+Sizes.padding);
        }

        public override void OnGUI(Rect rect)
        {
            EditorGUILayout.BeginHorizontal();

            EditorGUILayout.LabelField("Export " + (_isFinal ? "Final Value" : "Current Value") + " Statistics to CSV", Styles.textBold);

            GUIContent closeButton = new GUIContent(EditorGUIUtility.IconContent("winbtn_win_close"));

            if(GUILayout.Button(closeButton,GUILayout.Width(Sizes.Timeline.widthFileButton)))
            {
                editorWindow.Close();
            }

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();

            bool oldNumbers = _keepFileNumbers;
            _keepFileNumbers = EditorGUILayout.Toggle(new GUIContent("Keep File Numbers", "Adds a column with the current file number as shown in the statistics window for this statistic."), _keepFileNumbers);

            if(oldNumbers != _keepFileNumbers)
            {
                EditorPrefs.SetBool(_keepNumbersKey, _keepFileNumbers);
            }

            bool oldNames = _keepFileNames;
            _keepFileNames = EditorGUILayout.Toggle(new GUIContent("Keep File Names", "Adds a column with the current file name for this statistic."), _keepFileNames);

            if(oldNames != _keepFileNames)
            {
                EditorPrefs.SetBool(_keepNamesKey, _keepFileNames);
            }

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.LabelField("Example Headers and Row", Styles.textBold);

            EditorGUILayout.TextArea((_keepFileNumbers ? "FileNumber," : "") + (_keepFileNames ? "FileNames," : "") + _columnsRaw + 
                (_keepFileNumbers ? _exampleTextNumber : "") + (_keepFileNames ? _exampleTextName : "") + _exampleText,GUILayout.Height((Sizes.heightLine*2)-Sizes.padding));

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Columns: " + (_columns.Count + (_keepFileNumbers ? 1 : 0) + (_keepFileNames ? 1 : 0)).ToString());
            EditorGUILayout.LabelField("Rows: " + _totalRows.ToString());
            EditorGUILayout.EndHorizontal();
            if(GUILayout.Button("Export CSV"))
            {
                string name = System.DateTime.Now.ToString("yyyyMMddHHmmss") + " " + Application.productName.Replace(".", string.Empty) + " " + UnityEngine.SceneManagement.SceneManager.GetActiveScene().name + " Stats";
                string s = EditorUtility.SaveFilePanel("Export CSV", null, name, "csv");
                if(s.Length != 0)
                {
                    File.WriteAllText(s, GenerateCSV());
                }
            }
        }

        private string GetCSVLine(int fileIndex)
        {
            return GetCSVLine(fileIndex, _keepFileNumbers, _keepFileNames);
        }

        private string GetCSVLine(int fileIndex, bool keepFileNumber, bool keepFileName)
        {
            List<StatisticWindow.StatCache> tempCache = _statsCache.Where(x => x.fileIndex == fileIndex).ToList();
            
            if (tempCache.Count == 0)
                return "";

            string line = "";

            if (keepFileNumber)
            {
                line += tempCache[0].fileIndex.ToString() + ',';
            }

            if (keepFileName)
            {
                line += tempCache[0].fileName.ToCSVCell() + ',';
            }

            int findInd = 0;
            for (int i = 0; i < _columns.Count; i++)
            {
                findInd = tempCache.FindIndex(x => x.statName == _columns[i]);
                if(findInd == -1)
                {
                    line += ',';
                }
                else
                {
                    line += (_isFinal ? tempCache[findInd].final.ToCSVCell() : tempCache[findInd].current.ToCSVCell()) + ',';
                }
            }
            line = line.TrimEnd(',');
            line += "\n";
            return line;
        }

        private string GenerateCSV()
        {
            string output = "";
            output += (_keepFileNumbers ? "FileNumber," : "") + (_keepFileNames ? "FileNames," : "") + _columnsRaw;
            for (int i = 0; i < _fileIndexes.Count; i++)
            {
                output += GetCSVLine(_fileIndexes[i]);
            }
            return output;
        }
    }
}