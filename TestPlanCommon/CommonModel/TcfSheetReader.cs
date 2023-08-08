using Avago.ATF.StandardLibrary;
using System;
using System.Collections.Generic;

namespace TestPlanCommon.CommonModel
{
    public class TcfSheetReader
    {
        private int numRows;
        private int numColumns;
        public Tuple<bool, string, string[,]> allContents;
        public List<Dictionary<string, string>> testPlan;
        public List<string> Header;
        public int headerRow;
        public Dictionary<string, string> TableVertical;

        public TcfSheetReader(string sheetName, int numRows, int numCols)
        {
            this.numRows = numRows;
            this.numColumns = numCols;

            allContents = ATFCrossDomainWrapper.Excel_Get_IputRangeByValue(sheetName, 1, 1, numRows, numCols);

            if (allContents.Item1 == false)
            {
                throw new Exception("Error reading Excel Range\n\n" + allContents.Item2);
            }

            GetHeader();
            GetTestPlan();
        }

        public void Create(string sheetName, int numRows, int numCols)
        {
            this.numRows = numRows;
            this.numColumns = numCols;

            allContents = ATFCrossDomainWrapper.Excel_Get_IputRangeByValue(sheetName, 1, 1, numRows, numCols);

            if (allContents.Item1 == false)
            {
                throw new Exception("Error reading Excel Range\n\n" + allContents.Item2);
            }

            GetHeader();
            GetTestPlan();
        }

        public void CreateTableVertical(string sheetName)
        {
            this.numRows = 100;
            this.numColumns = 10;
            allContents = ATFCrossDomainWrapper.Excel_Get_IputRangeByValue(sheetName, 1, 1, numRows, numColumns);

            if (allContents.Item1 == false)
            {
                throw new Exception("Error reading Excel Range\n\n" + allContents.Item2);
            }

            TableVertical = new Dictionary<string, string>();

            int iStartRow = 0;
            int iStopRow = 0;
            for (int row = 1; row < numRows; row++)
            {
                string enableCell = allContents.Item3[row, 0];
                if (enableCell.ToUpper() == "#START")
                {
                    iStartRow = row + 1;
                }
                if (enableCell.ToUpper() == "#END")
                {
                    iStopRow = row;
                }
            }

            for (int row = iStartRow; row < iStopRow; row++)
            {
                string value = allContents.Item3[row, 1];
                string headerName = allContents.Item3[row, 0].Trim();
                TableVertical.Add(headerName, value);
            }
        }

        private void GetHeader()
        {
            Header = new List<string>();

            for (int row = 1; row < numRows; row++)
            {
                string enableCell = allContents.Item3[row, 0];

                if (enableCell.ToUpper() == "#START")
                {
                    headerRow = row;

                    for (int column = 0; column < numColumns; column++)
                    {
                        string value = allContents.Item3[row, column];
                        Header.Add(value.Trim());
                        if (value.ToUpper() == "#END") break;
                    }
                    break;
                }
            }
        }

        private void GetTestPlan()
        {
            testPlan = new List<Dictionary<string, string>>();
            List<Dictionary<string, string>> testPlanLoop = new List<Dictionary<string, string>>();

            bool loopStart = false;
            int numLoop = 0;

            for (int row = headerRow + 1; row < numRows; row++)
            {
                string enableCell = allContents.Item3[row, 0].Trim().ToUpper();
                string TestModeCell = allContents.Item3[row, 1].Trim().ToUpper();

                if (enableCell == "#END")
                {
                    if (loopStart == true && testPlanLoop.Count > 0)
                    {
                        testPlan.AddRange(testPlanLoop);
                        MPAD_TestTimer.LoggingManager.Instance.LogWarningTestPlan("Not found 'END LOOP', ignore loop logic");
                    }
                    break;
                }

                if (enableCell.StartsWith("LOOP"))
                {
                    if (loopStart == true) throw new Exception("Single loop syntax allowed, check your TCF");

                    loopStart = true;
                    var strnumLoop = enableCell.Split(' ');

                    if (strnumLoop.Length == 2 && int.TryParse(strnumLoop[1], out numLoop)) { }
                    else numLoop = 1;
                }
                else if (enableCell.StartsWith("END LOOP"))
                {
                    loopStart = false;
                }

                if (enableCell != "X" & TestModeCell != "")
                {
                    Dictionary<string, string> currentTestDict = new Dictionary<string, string>();

                    for (int i = 0; i < Header.Count; i++)
                    {
                        if (Header[i] != "")
                        {
                            string value = allContents.Item3[row, i];
                            currentTestDict.Add(Header[i], value);
                        }
                    }

                    if (loopStart == true)
                    {
                        testPlanLoop.Add(currentTestDict);
                    }
                    else
                    {
                        if (testPlanLoop.Count > 0)
                        {
                            testPlanLoop.Add(currentTestDict);
                            Dictionary<int, string> ParameterNoteValue = new Dictionary<int, string>();
                            bool isFirst = true;
                            int j = 0;

                            for (int i = 0; i < numLoop; i++)
                            {
                                List<Dictionary<string, string>> newList = new List<Dictionary<string, string>>();

                                foreach (var dict in testPlanLoop)
                                {
                                    newList.Add(new Dictionary<string, string>(dict));
                                }

                                newList.ForEach(s =>
                                {
                                    if (s.ContainsKey("ParameterNote"))
                                    {
                                        if (isFirst)
                                        {
                                            ParameterNoteValue.Add(j++, s["ParameterNote"]);
                                            s["ParameterNote"] = string.Format("{0}_LOOP-{1}", s["ParameterNote"], i + 1);
                                        }
                                        else
                                        {
                                            s["ParameterNote"] = string.Format("{0}_LOOP-{1}", ParameterNoteValue[j++], i + 1);
                                        }
                                    }
                                });

                                testPlan.AddRange(newList);

                                isFirst = false;
                                j = 0;
                            }

                            testPlanLoop.Clear();
                        }
                        else
                        {
                            testPlan.Add(currentTestDict);
                        }
                    }
                }
            }
        }

#if false
        public Dictionary.Ordered<string, string[]> GetDcResourceDefinitions()
        {
            Dictionary.Ordered<string, string[]> DcResourceTempList = new Dictionary.Ordered<string, string[]>();

            for (int col = 0; col < Header.Count; col++)
            {
                string head = Header[col];

                if (head.ToUpper().StartsWith("V."))
                {
                    string dcPinName = head.Replace("V.", "");

                    DcResourceTempList[dcPinName] = new string[Eq.NumSites];

                    for (byte site = 0; site < Eq.NumSites; site++)
                    {
                        DcResourceTempList[dcPinName][site] = allContents.Item3[headerRow - 1 - site, col].Trim();
                    }
                }
            }

            return DcResourceTempList;
        }

#endif
    }
}