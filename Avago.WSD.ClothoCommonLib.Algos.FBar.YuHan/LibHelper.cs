using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics; 

using Avago.ATF.Shares; 


namespace Avago.WSD.ClothoCommonLib.Algos.FBar.YuHan
{
    public class LibHelper
    {
        private  static readonly char[] tokens = { '+', '-', '*', '/' }; 
        
        // "Tno_";
        private readonly static int TagTnoLength = 4;

        private readonly static string TagFormula = "Formula";

        private readonly static string TagAdd = "Add";
        private const int TypeAdd = 1;
        private readonly static string TagSubstract = "Substract";
        private const int TypeSubstract = 2;
        private readonly static string TagMultiple = "Multiply";
        private const int TypeMultiple = 3;
        private readonly static string TagDivide = "Divide";
        private const int TypeDivide = 4;



        /// <summary>
        /// For Result-Independent case (No duplicated "Test_Number" Lines in TCF Entry
        /// It's possible to convert string to Dictionary
        /// </summary>
        /// <param name="input"></param>
        /// <param name="splittoken"></param>
        /// <returns></returns>
        public static Dictionary<string, string> ConvertListToDictionary(List<string> input, char splittoken)
        {
            Dictionary<string, string> ret = new Dictionary<string, string>();
            foreach (string line in input)
            {
                string[] items = line.Trim().Split(splittoken);
                if (items.Length != 2)
                    throw new Exception("Expect 'ParameterName=ParameterValue' Format: " + line);
                ret.Add(items[0].Trim(), items[1].Trim());
            }
            return ret; 
        }


        #region For tracing 

        [Conditional("Debug")]
        public static void PopulateDictionary(Dictionary<string, string> input)
        {
            foreach (string key in input.Keys)
                Trace.WriteLine(string.Format("     {0} = {1}", key, input[key])); 
        }

        [Conditional("Debug")]
        public static void PopulateList(List<string> input)
        {
            foreach (string line in input)
                Trace.WriteLine(string.Format("     {0}", line)); 
        }

        [Conditional("Debug")]
        public static void PopulateTags(Dictionary<int, TCFTag> input)
        {
            foreach(TCFTag tag in input.Values)            
                Trace.WriteLine(string.Format("     {0}", tag.ToString())); 
        }
        
        
        #endregion 
        
        
        
        #region Handling the local input parameters need evaluation case 
        

        ///     #Test_Number=37
        ///     Measurement=ProcessResults
        ///     SumChannel=DC_Set_BP+RL_Tx_BP_1848_1912   ' Simulate sum of Result #35 and #36
        ///     Header=ProcessDC_Set_BPAndRL_Tx_BP_1848_1912
        ///     #End
        ///     

        /// <summary>
        /// Evaluate whole Local Input Parameters 
        /// </summary>
        /// <param name="locals"></param>
        /// <param name="history"></param>
        /// <returns></returns>
        public static List<string> EvaluateLocalInputParametersIfNeed(List<string> locals, Dictionary<int, TCFTag> history)
        {
            string[] lines = locals.ToArray<string>(); 
            
            for(int idx=0; idx < lines.Length; idx++)
            {
                // this is a line to be evaluated 
                if(lines[idx].IndexOfAny(tokens) != -1)
                {
                    string[] pairs =lines[idx].Split(TestConditionFileConstant.EntrySplitTag); 
                    if(pairs.Length != 2)
                        throw new Exception("EvaluateLocalInputParameterFormulaIfAny: Invalid Parameter Line '" + lines + "'"); 

                    if(pairs[0].Trim().IndexOfAny(tokens) != -1)
                        throw new Exception("EvaluateLocalInputParameterFormulaIfAny: Invalid Parameter Name '" + lines + "'");

                    // Clear up any pre- or -post tokens
                    string thestr = pairs[1].Trim().Trim(tokens); 
                    if (thestr.IndexOfAny(tokens) != -1)
                    {
                        // definitely need evaluate
                        float thevalue = EvaluateFormulasToSingleValue(thestr, history);
                        lines[idx] = string.Format("{0}={1}", pairs[0].Trim(), thevalue);
                    }
                    else
                        lines[idx] = string.Format("{0}={1}", pairs[0].Trim(), thestr);
                }
                // Else, no need do anything
            }

            return new List<string>(lines); 
        }


        /// <summary>
        /// Evaluate a string of "formula" to value, based on history result
        /// </summary>
        /// <param name="formula">xxx = yyy + zzz - www </param>
        /// <param name="HistoryResult"></param>
        /// <returns></returns>
        public static float EvaluateFormulasToSingleValue(string formula, Dictionary<int, TCFTag> history)
        {
            string[] subitems = formula.Split(tokens);

            float result = 0f; 
            if(!getValueFromHistoryByHeader(subitems[0].Trim(), history, ref result)) 
                throw new Exception(string.Format("Capture Non-Exist Header '{0}' when Convert Formula '{1}' to Single Value",
                            subitems[0], formula)); 
            
            int startpos = subitems[0].Length;
            int tokenpos = -1;
            float tempret = 0f; 

            for(int idx = 1; idx < subitems.Length; idx++)
            {
                // From startpos, search for special char
                tokenpos = formula.IndexOfAny(tokens, startpos); 

                switch (formula[tokenpos])
                {
                    case '+':
                        if(!getValueFromHistoryByHeader(subitems[idx].Trim(), history, ref tempret)) 
                            throw new Exception(string.Format("Capture Non-Exist Header '{0}' when Convert Formula '{1}' to Single Value",
                                subitems[idx], formula));                                     
                        result += tempret; 
                        break;
                    case '-':
                        result -= tempret; 
                        break;
                    case '*':
                        result *= tempret; 
                        break;
                    case '/':
                        result /= tempret; 
                        break;
                    default:
                        throw new Exception(string.Format("Capture Un-Supported Token '{0}' when Convert Formula '{1}' to Single Value",
                            formula[tokenpos], formula)); 
                }

                startpos = tokenpos + 1 + subitems[idx].Length;
            }

            return result; 
        }

        
        private static bool getValueFromHistoryByHeader(string header, Dictionary<int, TCFTag> history, ref float ret)
        {
            bool found = false;
            bool assigned = false; 

            foreach (TCFTag tag in history.Values)
            {
                if(string.Compare(tag.Header, header, true) ==0)
                {
                    found = true;
                    assigned = tag.Processed; 
                    ret = tag.HeaderValue; 
                }
            }

            if(!assigned)
                throw new Exception(string.Format("Invalid to Refer to Header '{0}' before it's processed", header)); 

            return found; 
        }
        
        #endregion 


        
        #region Handling the TestNumber evaluation case 
        
        /// 
        ///     #Test_number=34		
        ///     Measurement=Calc
        ///     Test_number=Tno_32
        ///     Formula=Substract		'Addition, Substract, Multiplier, Divide
        ///     Test_number=Tno_33
        ///     Header=CalcNo32MinusNo33
        ///     #End
        /// 
        
        /// <summary>
        /// Calculate from "Test_Number/Formula" collection to get Header value
        /// </summary>
        /// <param name="items"></param>
        /// <param name="history"></param>
        /// <returns></returns>
        public static float EvaluateTestNumbersToSingleValue(List<string> Inputs, Dictionary<int, TCFTag> history)
        {
            float result = 0f;
            bool bExpectTestNumber = true; 

            int testnum = -1;
            int formula = -1; 

            foreach (string line in Inputs)
            {
                string[] items = line.Split(TestConditionFileConstant.EntrySplitTag);
                if (items.Length != 2)
                    throw new Exception(string.Format("Capture Invalid Line '{0}'", line)); 

                if(bExpectTestNumber)
                {
                    if (string.Compare(items[0].Trim(), TestConditionFileConstant.StartTag, true) != 0)
                    {
                        throw new Exception(string.Format("Capture Invalid Line '{0}': Expect TestNumber Line", line)); 
                    }

                    // Process test number 
                    try
                    {
                        testnum = Convert.ToInt32(items[1].Trim().Substring(TagTnoLength)); 
                    }
                    catch(Exception ex)
                    {
                        throw new Exception(string.Format("Capture Invalid TestNumber in Line '{0}': {1}", line, ex.Message)); 
                    }

                    float tempvalue = getValueFromHistoryByNum(testnum, history); 

                    if(formula == -1) 
                        // Init value 
                        result = tempvalue; 
                    else 
                    {
                        switch(formula)
                        {
                            case TypeAdd:
                                result += tempvalue; 
                                break; 
                            case TypeSubstract:
                                result -= tempvalue; 
                                break; 
                            case TypeMultiple:
                                result *= tempvalue; 
                                break; 
                            case TypeDivide:
                                result /= tempvalue; 
                                break; 
                            default: 
                                {
                                    throw new Exception(string.Format("Capture Invalid Formula in Line '{0}'. Abort Loading", line)); 
                                }
                        }
                    }
                }
                else 
                {
                    if (string.Compare(items[0].Trim(), TagFormula, true) != 0)
                    {
                        throw new Exception(string.Format("Capture Invalid Line '{0}': Expect Formula Line", line)); 
                    }

                    // Process formula
                    string typetag = items[1].Trim();
                    // Formulas
                    if (string.Compare(typetag, TagAdd, true) == 0)
                        formula = TypeAdd; 
                    else if(string.Compare(typetag, TagSubstract, true) == 0)
                        formula = TypeSubstract; 
                    else if(string.Compare(typetag, TagMultiple, true) == 0)
                        formula = TypeMultiple; 
                    else if(string.Compare(typetag, TagDivide, true) == 0)
                        formula = TypeDivide; 
                    else 
                    {
                        throw new Exception(string.Format("Capture Invalid Formula in Line '{0}'. Abort Loading", line)); 
                    }                    
                }

                bExpectTestNumber = !bExpectTestNumber; 
            }

            return result; 
        }


        private static float getValueFromHistoryByNum(int testnum, Dictionary<int, TCFTag> history)
        {
            if(!history[testnum].Processed)
                throw new Exception(string.Format("Invalid to Refer to Header '{0}' before it's processed", history[testnum].Header));
            return history[testnum].HeaderValue; 
        }

        #endregion 


    }
}
