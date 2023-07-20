using System.Linq;
using System.IO;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using PlayRecorder.Tools;
using System.Reflection;

namespace PlayRecorder.Statistics
{
    public class StatisticCSVPopup : PopupWindowContent
    {
        private float _width = 0f;
        private List<StatisticWindow.StatCache> _statsCache;

        private const string _columnsString = "Variable,VariableIndex,VariableTime,Value\n";
        private const int _columnsPreCount = 4;

        private string _exampleText = "";

        private bool _keepFileNumbers = true, _keepFileNames = true, _flattenMultiStats = false;

        private int _totalRows = 0;

        private const string _keepNumbersKey = "PlayRecorder_CSV_Numbers", _keepNamesKey = "PlayRecorder_CSV_Names", _flattenNamesKey = "PlayRecorder_CSV_Flatten";

        private Vector2 _scrollPos = Vector2.zero;

        public StatisticCSVPopup(List<StatisticWindow.StatCache> statsCache, float width)
        {
            _statsCache = statsCache;
            _width = width;

            if (EditorPrefs.HasKey(_keepNumbersKey))
            {
                _keepFileNumbers = EditorPrefs.GetBool(_keepNumbersKey);
            }
            if (EditorPrefs.HasKey(_keepNamesKey))
            {
                _keepFileNames = EditorPrefs.GetBool(_keepNamesKey);
            }
            if (EditorPrefs.HasKey(_flattenNamesKey))
            {
                _flattenMultiStats = EditorPrefs.GetBool(_flattenNamesKey);
            }

            for (int i = 0; i < _statsCache.Count; i++)
            {
                _totalRows += _statsCache[i].values.Length * (_flattenMultiStats ? 1 : _statsCache[i].statFields.Length);
            }

            _exampleText = GetCSVLines(_statsCache[0], 12).TrimEnd();
        }

        public override Vector2 GetWindowSize()
        {
            return new Vector2(_width - (Sizes.padding * 3), ((Sizes.heightLine + Sizes.padding) * 10));
        }

        public override void OnGUI(Rect rect)
        {
            EditorGUILayout.BeginHorizontal();

            EditorGUILayout.LabelField("Export Statistics to CSV", Styles.textBold);

            GUIContent closeButton = new GUIContent(EditorGUIUtility.IconContent("winbtn_win_close"));

            if (GUILayout.Button(closeButton, GUILayout.Width(Sizes.Timeline.widthFileButton)))
            {
                editorWindow.Close();
            }

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();

            bool oldNumbers = _keepFileNumbers;
            _keepFileNumbers = EditorGUILayout.Toggle(new GUIContent("Keep File Number", "Adds a column with the current file number as shown in the statistics window for this statistic."), _keepFileNumbers);

            if (oldNumbers != _keepFileNumbers)
            {
                EditorPrefs.SetBool(_keepNumbersKey, _keepFileNumbers);
                _exampleText = GetCSVLines(_statsCache[0], 12).TrimEnd();
            }

            bool oldNames = _keepFileNames;
            _keepFileNames = EditorGUILayout.Toggle(new GUIContent("Keep Recording Name", "Adds a column with the current recording name for this statistic."), _keepFileNames);

            if (oldNames != _keepFileNames)
            {
                EditorPrefs.SetBool(_keepNamesKey, _keepFileNames);
                _exampleText = GetCSVLines(_statsCache[0], 12).TrimEnd();
            }

            bool oldFlatten = _flattenMultiStats;
            _flattenMultiStats = EditorGUILayout.Toggle(new GUIContent("Flatten Stat Components", "By default, statistics with multiple parts will be stored as single row per element. (e.g. Vector3 XYZ -> Vector3_X Vector3_Y Vector3_Z)" +
                "This changes it to store it as a single row (e.g. Vector3 XYZ -> (X,Y,Z)"), _flattenMultiStats);

            if (oldFlatten != _flattenMultiStats)
            {
                EditorPrefs.SetBool(_flattenNamesKey, _flattenMultiStats);
                _exampleText = GetCSVLines(_statsCache[0], 12).TrimEnd();

                _totalRows = 0;
                for (int i = 0; i < _statsCache.Count; i++)
                {
                    _totalRows += _statsCache[i].values.Length * (_flattenMultiStats ? 1 : _statsCache[i].statFields.Length);
                }
            }

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.LabelField("Example Headers and Rows", Styles.textBold);

            _scrollPos = EditorGUILayout.BeginScrollView(_scrollPos, GUILayout.Height((Sizes.heightLine * 5) - Sizes.padding));

            EditorGUILayout.TextArea((_keepFileNumbers ? "FileNumber," : "") + (_keepFileNames ? "RecordingName," : "") + _columnsString +
                _exampleText, GUILayout.ExpandHeight(true));

            EditorGUILayout.EndScrollView();

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField($"Columns: {_columnsPreCount + (_keepFileNumbers ? 1 : 0) + (_keepFileNames ? 1 : 0)}");
            EditorGUILayout.LabelField($"Rows: {_totalRows}");
            EditorGUILayout.EndHorizontal();
            if (GUILayout.Button("Export CSV"))
            {
                string name = System.DateTime.Now.ToString("yyyyMMddHHmmss") + " " + Application.productName.Replace(".", string.Empty) + " " + UnityEngine.SceneManagement.SceneManager.GetActiveScene().name + " Stats";
                string s = EditorUtility.SaveFilePanel("Export CSV", null, name, "csv");
                if (s.Length != 0)
                {
                    File.WriteAllText(s, GenerateCSV());
                }
            }
        }

        private string GetCSVLines(StatisticWindow.StatCache cache, int trimLines = 0)
        {
            string line = "";

            int length = cache.values.Length;

            if (trimLines > 0 && cache.values.Length > trimLines)
            {
                length = trimLines;
            }

            for (int i = 0; i < length; i++)
            {
                if (cache.statFields.Length > 1 && !_flattenMultiStats)
                {
                    for (int y = 0; y < cache.statFields.Length; y++)
                    {
                        line += AddFileInfo(cache);
                        line += (cache.statName.ToCSVCell() + "_" + cache.statFields[y].Name).ToCSVCell() + ",";
                        line += i.ToString() + ",";
                        line += cache.statTimes[i].ToString() + ",";
                        line += cache.statFields[y].GetValue(cache.values[i]).ToString().ToCSVCell() + "\n";
                    }
                }
                else
                {
                    line += AddFileInfo(cache);
                    line += cache.statName.ToCSVCell() + ",";
                    line += i.ToString() + ",";
                    line += cache.statTimes[i].ToString() + ",";
                    if (cache.statFields.Length > 1 && _flattenMultiStats)
                    {
                        line += MultiStatSingleLine(cache.statFields, cache.values[i]).ToCSVCell() + "\n";
                    }
                    else
                    {
                        line += cache.values[i].ToString().ToCSVCell() + "\n";
                    }
                }
            }
            return line;
        }

        private string AddFileInfo(StatisticWindow.StatCache cache)
        {
            string s = "";
            if (_keepFileNumbers)
            {
                s += cache.fileIndex.ToString() + ',';
            }

            if (_keepFileNames)
            {
                s += cache.fileName.ToCSVCell() + ',';
            }
            return s;
        }

        private string MultiStatSingleLine(FieldInfo[] fields, object parentObject)
        {
            string output = "(";
            for (int i = 0; i < fields.Length; i++)
            {
                output += fields[i].GetValue(parentObject).ToString() + (i < fields.Length - 1 ? "," : "");
            }
            return output + ")";
        }

        private string GenerateCSV()
        {
            string output = "";
            output += (_keepFileNumbers ? "FileNumber," : "") + (_keepFileNames ? "RecordingName," : "") + _columnsString;
            for (int i = 0; i < _statsCache.Count; i++)
            {
                output += GetCSVLines(_statsCache[i]);
            }
            return output;
        }
    }
}