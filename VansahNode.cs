﻿using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using OpenQA.Selenium;
using System;
using System.Buffers.Text;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Reflection.PortableExecutable;
using System.Text;
using System.Text.Json.Nodes;
using static System.Net.Mime.MediaTypeNames;

namespace Vansah
{
    public class VansahNode
    {


        //--------------------------- ENDPOINTS -------------------------------------------------------------------------------
        private static string API_VERSION = "v1";
        private static string VANSAH_URL = "https://prod.vansahnode.app";
        private static string Add_TEST_RUN = VANSAH_URL + "/api/" + API_VERSION + "/run";
        private static string Add_TEST_LOG = VANSAH_URL + "/api/" + API_VERSION + "/logs";
        private static string UPDATE_TEST_LOG = VANSAH_URL + "/api/" + API_VERSION + "/logs/";
        private static string REMOVE_TEST_LOG = VANSAH_URL + "/api/" + API_VERSION + "/logs/";
        private static string REMOVE_TEST_RUN = VANSAH_URL + "/api/" + API_VERSION + "/run/";
        private static string TEST_SCRIPT = VANSAH_URL + "/api/" + API_VERSION + "/testCase/list/testScripts";
        //--------------------------------------------------------------------------------------------------------------------


        //--------------------------- INFORM YOUR UNIQUE VANSAH TOKEN HERE ---------------------------------------------------
        private static string VANSAH_TOKEN = "Your Vansah Token Here";


        //--------------------------- INFORM IF YOU WANT TO UPDATE VANSAH HERE -----------------------------------------------
        // 0 = NO RESULTS WILL BE SENT TO VANSAH
        // 1 = RESULTS WILL BE SENT TO VANSAH
        private static readonly string updateVansah = "1";
        //--------------------------------------------------------------------------------------------------------------------	


        //--------------------------------------------------------------------------------------------------------------------
        private string TESTFOLDERS_ID;  //Mandatory (GUID Test folder Identifer) Optional if issue_key is provided
        private string JIRA_ISSUE_KEY;  //Mandatory (JIRA ISSUE KEY) Optional if Test Folder is provided
        private string SPRINT_NAME; //Mandatory (SPRINT KEY)
        private string CASE_KEY;   //CaseKey ID (Example - TEST-C1) Mandatory
        private string RELEASE_NAME;  //Release Key (JIRA Release/Version Key) Mandatory
        private string ENVIRONMENT_NAME; //Enivronment ID from Vansah for JIRA app. (Example SYS or UAT ) Mandatory
        private int RESULT_KEY;    // Result Key such as (Result value. Options: (0 = N/A, 1= FAIL, 2= PASS, 3 = Not tested)) Mandatory
        private bool SEND_SCREENSHOT;   // true or false If Required to take a screenshot of the webPage that to be tested.
        private string COMMENT;  //Actual Result 	
        private int STEP_ORDER;   //Test Step index	
        private string TEST_RUN_IDENTIFIER; //To be generated by API request
        private string TEST_LOG_IDENTIFIER; //To be generated by API request
        private string FILE;
        private int testRows;
        private HttpClient httpClient;



        //------------------------ VANSAH INSTANCE CREATION---------------------------------------------------------------------------------
        //Creates an Instance of vansahnode, to set all the required field
        public VansahNode(string TESTFOLDERS, string jiraIssue)
        {
            TESTFOLDERS_ID = TESTFOLDERS;
            JIRA_ISSUE_KEY = jiraIssue;

        }
        //Default Constructor
        public VansahNode()
        {
        }

        //------------------------ VANSAH Add TEST RUN(TEST RUN IDENTIFIER CREATION) -------------------------------------------
        //POST prod.vansahnode.app/api/v1/run --> https://apidoc.vansah.com/#0ebf5b8f-edc5-4adb-8333-aca93059f31c
        //creates a new test run Identifier which is then used with the other testing methods: 1) Add_test_log 2) remove_test_run

        //For JIRA ISSUES
        public void AddTestRunFromJiraIssue(string testcase)
        {

            CASE_KEY = testcase;
            SEND_SCREENSHOT = false;

            ConnectToVansahRest("AddTestRunFromJiraIssue", null);
        }
        //For TestFolders
        public void AddTestRunFromTestFolder(string testcase)
        {

            CASE_KEY = testcase;
            SEND_SCREENSHOT = false;
            ConnectToVansahRest("AddTestRunFromTestFolder", null);
        }
        //------------------------------------------------------------------------------------------------------------------------



        //-------------------------- VANSAH Add TEST LOG (LOG IDENTIFIER CREATION ------------------------------------------------
        //POST prod.vansahnode.app/api/v1/logs --> https://apidoc.vansah.com/#8cad9d9e-003c-43a2-b29e-26ec2acf67a7
        //Adds a new test log for the test case_key. Requires "test_run_identifier" from Add_test_run

        public void AddTestLog(int result, string comment, int testStepRow, bool sendScreenShot, IWebDriver driver)
        {

            //0 = N/A, 1 = FAIL, 2 = PASS, 3 = Not tested
            RESULT_KEY = result;
            COMMENT = comment;
            STEP_ORDER = testStepRow;
            SEND_SCREENSHOT = sendScreenShot;
            ConnectToVansahRest("AddTestLog", driver);
        }
        //-------------------------------------------------------------------------------------------------------------------------



        //------------------------- VANSAH Add QUICK TEST --------------------------------------------------------------------------
        //POST prod.vansahnode.app/api/v1/run --> https://apidoc.vansah.com/#0ebf5b8f-edc5-4adb-8333-aca93059f31c
        //creates a new test run and a new test log for the test case_key. By calling this endpoint, 
        //you will create a new log entry in Vansah with the respective overal Result. 
        //(0 = N/A, 1= FAIL, 2= PASS, 3 = Not Tested). Add_Quick_Test is useful for test cases in which there are no steps in the test script, 
        //where only the overall result is important.

        //For JIRA ISSUES
        public void AddQuickTestFromJiraIssue(string testcase, int result)
        {

            //0 = N/A, 1= FAIL, 2= PASS, 3 = Not tested
            CASE_KEY = testcase;
            RESULT_KEY = result;

            ConnectToVansahRest("AddQuickTestFromJiraIssue", null);
        }
        //For TestFolders
        public void AddQuickTestFromTestFolders(string testcase, int result)
        {

            //0 = N/A, 1= FAIL, 2= PASS, 3 = Not tested
            CASE_KEY = testcase;
            RESULT_KEY = result;

            ConnectToVansahRest("AddQuickTestFromTestFolders", null);
        }

        //------------------------------------------------------------------------------------------------------------------------------


        //------------------------------------------ VANSAH REMOVE TEST RUN *********************************************
        //POST prod.vansahnode.app/api/v1/run/{{test_run_identifier}} --> https://apidoc.vansah.com/#2f004698-34e9-4097-89ab-759a8d86fca8
        //will delete the test log created from Add_test_run or Add_quick_test

        public void RemoveTestRun()
        {
            ConnectToVansahRest("RemoveTestRun", null);
        }
        //------------------------------------------------------------------------------------------------------------------------------

        //------------------------------------------ VANSAH REMOVE TEST LOG *********************************************
        //POST remove_test_log https://apidoc.vansah.com/#789414f9-43e7-4744-b2ca-1aaf9ee878e5
        //will delete a test_log_identifier created from Add_test_log or Add_quick_test

        public void RemoveTestLog()
        {
            ConnectToVansahRest("RemoveTestLog", null);
        }
        //------------------------------------------------------------------------------------------------------------------------------


        //------------------------------------------ VANSAH UPDATE TEST LOG ------------------------------------------------------------
        //POST update_test_log https://apidoc.vansah.com/#ae26f43a-b918-4ec9-8422-20553f880b48
        //will perform any updates required using the test log identifier which is returned from Add_test_log or Add_quick_test

        public void UpdateTestLog(int result, string comment, bool sendScreenShot, IWebDriver driver)
        {

            //0 = N/A, 1= FAIL, 2= PASS, 3 = Not tested
            RESULT_KEY = result;
            COMMENT = comment;
            SEND_SCREENSHOT = sendScreenShot;
            ConnectToVansahRest("UpdateTestLog", driver);
        }

        private void ConnectToVansahRest(string type, IWebDriver driver)
        {

            if (updateVansah == "1")
            {
                httpClient = new HttpClient();
                HttpResponseMessage response = null;
                JsonObject requestBody;
                HttpContent Content;

                //Adding headers
                httpClient.DefaultRequestHeaders.Accept.Clear();
                httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                httpClient.DefaultRequestHeaders.Add("Authorization", VANSAH_TOKEN);
                if (SEND_SCREENSHOT)
                {
                    Screenshot TakeScreenshot = ((ITakesScreenshot)driver).GetScreenshot();
                    FILE = TakeScreenshot.AsBase64EncodedString;

                }
                if (type == "AddTestRunFromJiraIssue")
                {

                    requestBody = new();
                    requestBody.Add("case", TestCase());
                    requestBody.Add("asset", JiraIssueAsset());
                    if (Properties().Count != 0) { requestBody.Add("properties", Properties()); }



                    // Console.WriteLine(requestBody);
                    httpClient.BaseAddress = new Uri(Add_TEST_RUN);

                    Content = new StringContent(requestBody.ToJsonString(), Encoding.UTF8, "application/json" /* or "application/json" in older versions */);
                    response = httpClient.PostAsync("", Content).Result;
                    //  Console.WriteLine(response.Content);

                }
                if (type == "AddTestRunFromTestFolder")
                {
                    requestBody = new();
                    requestBody.Add("case", TestCase());
                    requestBody.Add("asset", TestFolderAsset());
                    if (Properties().Count != 0) { requestBody.Add("properties", Properties()); }

                    //Console.WriteLine(requestBody);
                    httpClient.BaseAddress = new Uri(Add_TEST_RUN);

                    Content = new StringContent(requestBody.ToJsonString(), Encoding.UTF8, "application/json" /* or "application/json" in older versions */);
                    response = httpClient.PostAsync("", Content).Result;


                }
                if (type == "AddTestLog")
                {
                    requestBody = AddTestLogProp();
                    if (SEND_SCREENSHOT)
                    {
                        JsonArray array = new();
                        array.Add(AddAttachment(FileName()));

                        requestBody.Add("attachments", array);


                    }

                    //      Console.WriteLine(requestBody);
                    httpClient.BaseAddress = new Uri(Add_TEST_LOG);



                    Content = new StringContent(requestBody.ToJsonString(), Encoding.UTF8, "application/json" /* or "application/json" in older versions */);
                    response = httpClient.PostAsync("", Content).Result;

                }
                if (type == "AddQuickTestFromJiraIssue")
                {

                    requestBody = new();
                    requestBody.Add("case", TestCase());
                    requestBody.Add("asset", JiraIssueAsset());
                    if (Properties().Count != 0)
                    {
                        requestBody.Add("properties", Properties());
                    }
                    requestBody.Add("result", resultObj(RESULT_KEY));
                    if (SEND_SCREENSHOT)
                    {
                        JsonArray array = new();
                        array.Add(AddAttachment(FileName()));

                        requestBody.Add("attachments", array);
                    }

                    Console.WriteLine(requestBody);
                    httpClient.BaseAddress = new Uri(Add_TEST_RUN);



                    Content = new StringContent(requestBody.ToJsonString(), Encoding.UTF8, "application/json" /* or "application/json" in older versions */);
                    response = httpClient.PostAsync("", Content).Result;

                }
                if (type == "AddQuickTestFromTestFolders")
                {
                    requestBody = new();
                    requestBody.Add("case", TestCase());
                    requestBody.Add("asset", TestFolderAsset());
                    if (Properties().Count != 0)
                    {
                        requestBody.Add("properties", Properties());
                    }
                    requestBody.Add("result", resultObj(RESULT_KEY));
                    if (SEND_SCREENSHOT)
                    {
                        JsonArray array = new();
                        array.Add(AddAttachment(FileName()));

                        requestBody.Add("attachments", array);
                    }



                    Console.WriteLine(requestBody);
                    httpClient.BaseAddress = new Uri(Add_TEST_RUN);



                    Content = new StringContent(requestBody.ToJsonString(), Encoding.UTF8, "application/json" /* or "application/json" in older versions */);
                    response = httpClient.PostAsync("", Content).Result;

                }
                if (type == "RemoveTestRun")
                {
                    //Console.WriteLine(requestBody);
                    httpClient.BaseAddress = new Uri(REMOVE_TEST_RUN + TEST_RUN_IDENTIFIER);
                    response = httpClient.DeleteAsync("").Result;
                }


                if (type == "RemoveTestLog")
                {
                    //Console.WriteLine(requestBody);
                    httpClient.BaseAddress = new Uri(REMOVE_TEST_LOG + TEST_LOG_IDENTIFIER);
                    response = httpClient.DeleteAsync("").Result;
                }


                if (type == "UpdateTestLog")
                {
                    requestBody = new();

                    requestBody.Add("result", resultObj(RESULT_KEY));
                    requestBody.Add("actualResult", COMMENT);
                    if (SEND_SCREENSHOT)
                    {
                        JsonArray array = new();
                        array.Add(AddAttachment(FileName()));

                        requestBody.Add("attachments", array);
                    }
                    //Console.WriteLine(requestBody);
                    httpClient.BaseAddress = new Uri(UPDATE_TEST_LOG + TEST_LOG_IDENTIFIER);
                    Content = new StringContent(requestBody.ToJsonString(), Encoding.UTF8, "application/json" /* or "application/json" in older versions */);
                    response = httpClient.PutAsync("", Content).Result;
                }

                if (response.IsSuccessStatusCode)
                {
                    //Console.WriteLine(response.RequestMessage);

                    var responseMessage = response.Content.ReadAsStringAsync().Result;
                    var obj = JObject.Parse(responseMessage);
                    //Console.WriteLine(obj);
                    if (type == "AddTestRunFromJiraIssue")
                    {

                        TEST_RUN_IDENTIFIER = obj.SelectToken("data.run.identifier").ToString();
                        Console.WriteLine($"Test Run has been created Successfully RUN ID : {TEST_RUN_IDENTIFIER}");

                    }
                    if (type == "AddTestRunFromTestFolder")
                    {
                        TEST_RUN_IDENTIFIER = obj.SelectToken("data.run.identifier").ToString();
                        Console.WriteLine($"Test Run has been created Successfully RUN ID : {TEST_RUN_IDENTIFIER}");
                    }
                    if (type == "AddTestLog")
                    {
                        TEST_LOG_IDENTIFIER = obj.SelectToken("data.log.identifier").ToString();
                        Console.WriteLine($"Test Log has been Added to a test Step Successfully LOG ID : {TEST_LOG_IDENTIFIER}");

                    }
                    if (type == "AddQuickTestFromJiraIssue")
                    {

                        string message = obj.SelectToken("message").ToString();
                        Console.WriteLine($"Quick Test : {message}");

                    }
                    if (type == "AddQuickTestFromTestFolders")
                    {

                        string message = obj.SelectToken("message").ToString();
                        Console.WriteLine($"Quick Test : {message}");

                    }
                    if (type == "RemoveTestLog")
                    {
                        Console.WriteLine($"Test Log has been removed from a test Step Successfully LOG ID : {TEST_LOG_IDENTIFIER}");
                    }
                    if (type == "RemoveTestRun")
                    {
                        Console.WriteLine($"Test Run has been removed Successfully for the testCase : {CASE_KEY} RUN ID : {TEST_RUN_IDENTIFIER}");

                    }
                    response.Dispose();

                }
                else
                {
                    var responseMessage = response.Content.ReadAsStringAsync().Result;
                    var obj = JObject.Parse(responseMessage);
                    // Console.WriteLine(obj);
                    Console.WriteLine(obj.SelectToken("message").ToString());
                    response.Dispose();
                }

            }
            else
            {
                Console.WriteLine("Sending Test Results to Vansah TM for JIRA is Disabled");
            }
        }
        //Setter and Getter's 
        //To Set the TestFolderID 
        public void SetTESTFOLDERS_ID(string tESTFOLDERS_ID)
        {
            this.TESTFOLDERS_ID = tESTFOLDERS_ID;
        }

        //To Set the JIRA_ISSUE_KEY
        public void SetJIRA_ISSUE_KEY(string jIRA_ISSUE_KEY)
        {
            this.JIRA_ISSUE_KEY = jIRA_ISSUE_KEY;
        }

        //To Set the SPRINT_NAME
        public void SetSPRINT_NAME(string SPRINT_NAME)
        {
            this.SPRINT_NAME = SPRINT_NAME;
        }

        //To Set the RELEASE_NAME
        public void SetRELEASE_NAME(string RELEASE_NAME)
        {
            this.RELEASE_NAME = RELEASE_NAME;
        }

        //To Set the ENVIRONMENT_NAME
        public void SetENVIRONMENT_NAME(string ENVIRONMENT_NAME)
        {
            this.ENVIRONMENT_NAME = ENVIRONMENT_NAME;
        }

        //JsonObject - Test Run Properties 
        private JsonObject Properties()
        {
            JsonObject environment = new();
            environment.Add("name", ENVIRONMENT_NAME);

            JsonObject release = new();
            release.Add("name", RELEASE_NAME);

            JsonObject sprint = new();
            sprint.Add("name", SPRINT_NAME);

            JsonObject Properties = new();
            if (SPRINT_NAME != null)
            {
                if (SPRINT_NAME.Length >= 2)
                {
                    Properties.Add("sprint", sprint);
                }
            }
            if (RELEASE_NAME != null)
            {
                if (RELEASE_NAME.Length >= 2)
                {
                    Properties.Add("release", release);
                }
            }
            if (ENVIRONMENT_NAME != null)
            {
                if (ENVIRONMENT_NAME.Length >= 2)
                {
                    Properties.Add("environment", environment);
                }
            }

            return Properties;
        }


        //JsonObject - To Add TestCase Key
        private JsonObject TestCase()
        {

            JsonObject testCase = new();
            if (CASE_KEY != null)
            {
                if (CASE_KEY.Length >= 2)
                {
                    testCase.Add("key", CASE_KEY);
                }
            }
            else
            {
                Console.WriteLine("Please Provide Valid TestCase Key");
            }

            return testCase;
        }
        //JsonObject - To Add Result ID
        private JsonObject resultObj(int result)
        {

            JsonObject resultID = new();

            resultID.Add("id", result);


            return resultID;
        }
        //JsonObject - To Add JIRA Issue name
        private JsonObject JiraIssueAsset()
        {

            JsonObject asset = new();
            if (JIRA_ISSUE_KEY != null)
            {
                if (JIRA_ISSUE_KEY.Length >= 2)
                {
                    asset.Add("type", "issue");
                    asset.Add("key", JIRA_ISSUE_KEY);
                }
            }
            else
            {
                Console.WriteLine("Please Provide Valid JIRA Issue Key");
            }


            return asset;
        }
        //JsonObject - To Add TestFolder ID 
        private JsonObject TestFolderAsset()
        {

            JsonObject asset = new();
            if (TESTFOLDERS_ID != null)
            {
                if (TESTFOLDERS_ID.Length >= 2)
                {
                    asset.Add("type", "folder");
                    asset.Add("identifier", TESTFOLDERS_ID);
                }
            }
            else
            {
                Console.WriteLine("Please Provide Valid TestFolder ID");
            }


            return asset;
        }

        //JsonObject - To AddTestLog
        private JsonObject AddTestLogProp()
        {

            JsonObject testRun = new();
            testRun.Add("identifier", TEST_RUN_IDENTIFIER);

            JsonObject stepNumber = new();
            stepNumber.Add("number", STEP_ORDER);

            JsonObject testResult = new();
            testResult.Add("id", RESULT_KEY);

            JsonObject testLogProp = new();

            testLogProp.Add("run", testRun);

            testLogProp.Add("step", stepNumber);

            testLogProp.Add("result", testResult);

            testLogProp.Add("actualResult", COMMENT);


            return testLogProp;
        }
        //JsonObject - To Add Add Attachments to a Test Log
        private JsonObject AddAttachment(string File)
        {

            JsonObject attachmentsInfo = new();
            attachmentsInfo.Add("name", File);
            attachmentsInfo.Add("extension", "png");
            //Console.WriteLine(attachmentsInfo);
            attachmentsInfo.Add("file", FILE);

            return attachmentsInfo;

        }

        //Set FileName
        private string FileName()
        {

            string filename = Path.GetRandomFileName().Replace(".", "");

            return filename;
        }


    }
}



