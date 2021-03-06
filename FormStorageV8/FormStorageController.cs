﻿using ExpandoJsonMvcStub.Helpers;

using FormStorage.Models;

using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Text;
using System.Web.Configuration;
using System.Web.Mvc;
using System.Web.Script.Serialization;

using Umbraco.Core;
using Umbraco.Core.Composing;
using Umbraco.Core.Logging;
using Umbraco.Core.Persistence;
using Umbraco.Core.Scoping;
using Umbraco.Web.Mvc;

namespace FormStorage.Controllers
{

    public class FormStorageController : SurfaceController
	{
        private List<dynamic> GenerateSubmissionData(string alias, out string fieldNames)
        {
            List<dynamic> resultList = new List<dynamic>();
            fieldNames = "";

            int formID = FormSchema.GetFormIDFromAlias(alias, false);
            if (formID > -1)
            {
                using (IScope scope = Current.ScopeProvider.CreateScope())
                {
                    IUmbracoDatabase DatabaseConnection = scope.Database;

                    List<FormStorageSubmissionModel> fetchedSubmissionRecords = new List<FormStorageSubmissionModel>();
                    string querySQL = "SELECT * FROM FormStorageSubmissions WHERE formID = @formID";

                    string filterValue = Request.Params["period"];
                    DateTime filterDate = DateTime.Now.Date;
                    if (!string.IsNullOrEmpty(filterValue))
                    {
                        int filterDays = 0;
                        int.TryParse(filterValue, out filterDays);
                        filterDate = filterDate.AddDays(filterDays * -1);
                    }

                    try
                    {
                        if (!string.IsNullOrEmpty(filterValue))
                        {
                            querySQL += " AND datetime >= @datetime";
                            fetchedSubmissionRecords = DatabaseConnection.Query<FormStorageSubmissionModel>(querySQL, new { formID = formID, datetime = filterDate }).ToList();
                        }
                        else
                        {
                            fetchedSubmissionRecords = DatabaseConnection.Query<FormStorageSubmissionModel>(querySQL, new { formID = formID }).ToList();
                        }
                    }
                    catch (Exception ex)
                    {
                        Logger.Error<FormStorageController>("Unable to query FormStorageSubmissions table : {message}", ex.Message);
                        return resultList;
                    }

                    // Set-up filters
                    List<string> fieldList = null;
                    List<string> filterFieldList = new List<string>();
                    fieldNames = WebConfigurationManager.AppSettings["FormStorage:" + alias];
                    if (!string.IsNullOrEmpty(fieldNames))
                    {
                        fieldList = new List<string>(fieldNames.Split(','));
                        fieldList.Add("IP");
                        fieldList.Add("datetime");
                    }
                    bool filterApplied = false;
                    if (fieldList != null)
                    {
                        foreach (string fieldName in fieldList)
                        {
                            if (Request.Params[fieldName] != null)
                            {
                                filterFieldList.Add(fieldName);
                                filterApplied = true;
                            }
                        }
                    }

                    foreach (FormStorageSubmissionModel currentSubmissionRecord in fetchedSubmissionRecords)
                    {
                        List<FormStorageEntryModel> fetchedEntryRecords = new List<FormStorageEntryModel>();
                        querySQL = "SELECT * FROM FormStorageEntries WHERE submissionID = @submissionID";
                        try
                        {
                            fetchedEntryRecords = DatabaseConnection.Query<FormStorageEntryModel>(querySQL, new { submissionID = currentSubmissionRecord.SubmissionID }).ToList();
                        }
                        catch (Exception ex)
                        {
                            Logger.Error<FormStorageController>("Unable to query FormStorageSubmissions table : {message}", ex.Message);
                            return resultList;
                        }

                        var currentRecord = new ExpandoObject() as IDictionary<string, Object>;
                        currentRecord.Add("submissionID", currentSubmissionRecord.SubmissionID);
                        currentRecord.Add("datetime", currentSubmissionRecord.Datetime);
                        currentRecord.Add("IP", currentSubmissionRecord.IP);
                        foreach (FormStorageEntryModel formStorageEntry in fetchedEntryRecords)
                        {
                            currentRecord.Add(formStorageEntry.FieldAlias, formStorageEntry.Value);
                        }

                        // Apply filters...
                        bool filteredOut = false;
                        if (filterApplied)
                        {
                            foreach (string fieldName in filterFieldList)
                            {
                                if (((IDictionary<string, Object>)currentRecord).ContainsKey(fieldName))
                                {
                                    filterValue = Request.Params[fieldName];
                                    if (!currentRecord[fieldName].ToString().ToUpper().Contains(filterValue.ToUpper()))
                                    {
                                        filteredOut = true;
                                        break;
                                    }
                                }
                            }
                        }
                        if (!filteredOut) { resultList.Add(currentRecord); }
                    }
                    resultList = HandleListSorting(resultList);

                    scope.Complete();
                }
            }
            return resultList;
        }

        private List<dynamic> HandleListPagination(List<dynamic> inputList)
        {
            List<dynamic> outputList = inputList;
            if ((Request.Params["pageIndex"] != null) && (Request.Params["pageSize"] != null))
            {
                int pageIndex = 0;
                int.TryParse(Request.Params["pageIndex"], out pageIndex);
                int pageSize = 0;
                int.TryParse(Request.Params["pageSize"], out pageSize);
                if ((pageIndex > 0) && (pageSize > 0))
                {
                    int rangeStart = (pageIndex * pageSize) - pageSize;
                    if ((rangeStart + pageSize) > inputList.Count)
                    {
                        pageSize = inputList.Count - rangeStart;
                    }
                    outputList = inputList.GetRange(rangeStart, pageSize);
                }
            }
            return outputList;
        }

        private List<dynamic> HandleListSorting(List<dynamic> inputList)
        {
            List<dynamic> outputList = inputList;
            if ((Request.Params["sortField"] != null) && (Request.Params["sortOrder"] != null))
            {
                if (Request.Params["sortOrder"].ToUpper() == "DESC")
                {
                    outputList = inputList.OrderByDescending(x => ((IDictionary<string, object>)x)[Request.Params["sortField"]]).ToList();
                }
                else
                {
                    outputList = inputList.OrderBy(x => ((IDictionary<string, object>)x)[Request.Params["sortField"]]).ToList();
                }
            }
            return outputList;
        }

        [HttpDelete]
        public ActionResult DeleteFormSubmissionRecord(int submissionID)
		{
            bool success = true;
            using (IScope scope = Current.ScopeProvider.CreateScope())
            {
                IUmbracoDatabase DatabaseConnection = scope.Database;
                try
                {
                    DatabaseConnection.Execute("DELETE FROM FormStorageSubmissions WHERE submissionID=@0", submissionID);
                }
                catch (Exception ex)
                {
                    Logger.Error<FormStorageController>("Unable to delete from FormStorageSubmissions table : {message}", ex.Message);
                    success = false;
                }
                if (success)
                {
                    try
                    {
                        DatabaseConnection.Execute("DELETE FROM FormStorageEntries WHERE submissionID=@0", submissionID);
                    }
                    catch (Exception ex)
                    {
                        Logger.Error<FormStorageController>("Unable to delete from FormStorageSubmissions table : {message}", ex.Message);
                        success = false;
                    }
                }
                scope.Complete();
            }
            var result = new
            {
                success = success,
            };
            JavaScriptSerializer javaScriptSerializer = new JavaScriptSerializer();
            string resultJSON = javaScriptSerializer.Serialize(result);
            return Content(resultJSON, "application/json");
        }

        [HttpGet]
        public FileResult DownloadFormSubmissions(string alias, string filename)
        {
            string resultData = "";
            string fieldNames = "";
            List<dynamic> resultList = GenerateSubmissionData(alias, out fieldNames);
            if ((resultList.Count > 0) && (!string.IsNullOrEmpty(fieldNames)))
            {
                string headerLine = "Date/Time".WrapQuotes() + "," + "IP".WrapQuotes() + ",";
                foreach (string currentFieldName in fieldNames.Split(','))
                {
                    string fieldTitle = WebConfigurationManager.AppSettings["FormStorage:Translation:" + currentFieldName];
                    if (string.IsNullOrEmpty(fieldTitle))
                    {
                        headerLine += currentFieldName.WrapQuotes() + ",";
                    }
                    else
                    {
                        headerLine += fieldTitle.WrapQuotes() + ",";
                    }
                }
                headerLine = headerLine.Substring(0, headerLine.Length - 1);
                resultData += headerLine + "\n";

                foreach (IDictionary<string, Object> currentRecord in resultList)
                {
                    string currentLine = ((DateTime)currentRecord["datetime"]).ToString("MMM dd, yyyy HH:mm tt").WrapQuotes() + ",";
                    currentLine += currentRecord["IP"].ToString().WrapQuotes() + ",";
                    foreach (string currentFieldName in fieldNames.Split(','))
                    {
                        if (currentRecord.ContainsKey(currentFieldName))
                        {
                            currentLine += currentRecord[currentFieldName].ToString().WrapQuotes() + ",";
                        }
                        else
                        {
                            currentLine += "\"\",";
                        }
                    }
                    currentLine = currentLine.Substring(0, currentLine.Length - 1);
                    resultData += currentLine + "\n";
                }
            }
            string downloadfileName = alias + ".csv";
            if (!string.IsNullOrEmpty(filename)) { downloadfileName = filename + ".csv"; }

            byte[] byteData = Encoding.UTF8.GetBytes(resultData);
            byte[] fileContents = Encoding.UTF8.GetPreamble().Concat(byteData).ToArray();

            System.Net.Mime.ContentDisposition cd = new System.Net.Mime.ContentDisposition
            {
                FileName = downloadfileName,
                Inline = false
            };
            Response.Headers.Add("Content-Disposition", cd.ToString());
            Response.Headers.Add("X-Content-Type-Options", "nosniff");

            return File(fileContents, "text/csv");
        }

        [HttpGet]
        public ActionResult FetchFormSubmissionData(string alias)
        {
            string fieldNames = "";
            List<dynamic> resultList = GenerateSubmissionData(alias, out fieldNames);

            int totalCount = resultList.Count;
            resultList = HandleListPagination(resultList);

            var dataResult = new
            {
                data = resultList.ToArray(),
                itemsCount = totalCount,
            };

            JavaScriptSerializer javaScriptSerializer = new JavaScriptSerializer();
            javaScriptSerializer.RegisterConverters(new JavaScriptConverter[] { new ExpandoJsonConverter() });
            string resultJSON = javaScriptSerializer.Serialize(dataResult);
            return Content(resultJSON, "application/json");
        }

		[HttpGet]
		public ActionResult GetFieldsForAlias(string alias)
		{
			var fieldList = new List<dynamic>();
			string fieldNames = WebConfigurationManager.AppSettings["FormStorage:" + alias];
			if (!string.IsNullOrEmpty(fieldNames))
			{
				foreach (string fieldName in fieldNames.Split(','))
				{
					string fieldTitle = WebConfigurationManager.AppSettings["FormStorage:Translation:" + fieldName];
					if (string.IsNullOrEmpty(fieldTitle)) { fieldTitle = fieldName; }

					int fieldWidth = 100;
					string fieldWidthValue = WebConfigurationManager.AppSettings["FormStorage:" + alias + ":" + fieldName + ":width"];
					if (!string.IsNullOrEmpty(fieldWidthValue)) { int.TryParse(fieldWidthValue, out fieldWidth); }

                    string[] fieldAlignValues = { "left", "center", "right" };
                    string fieldAlign = "center";
                    string fieldAlignValue = WebConfigurationManager.AppSettings["FormStorage:" + alias + ":" + fieldName + ":align"];
                    if ((!string.IsNullOrEmpty(fieldAlignValue)) && (fieldAlignValues.Contains(fieldAlignValue.ToLower())))
                    {
                        fieldAlign = fieldAlignValue.ToLower();
                    }

                    bool fieldVisible = true;
                    string fieldVisibleValue = WebConfigurationManager.AppSettings["FormStorage:" + alias + ":" + fieldName + ":visible"];
                    if (!string.IsNullOrEmpty(fieldVisibleValue)) { fieldVisible = !(fieldVisibleValue.ToLower() == "false"); }

                    dynamic currentField = new
					{
						name = fieldName,
						title = fieldTitle,
						type = "text",
						width = fieldWidth,
                        align = fieldAlign,
                        visible = fieldVisible
                    };
					fieldList.Add(currentField);
				}
			}
			else
			{
                Logger.Error<FormStorageController>("FormStorageController::GetFieldsForAlias - \"FormStorage:{alias}\" not found in web.config.", alias);
            }
			return Json(fieldList.ToArray(), JsonRequestBehavior.AllowGet);
		}

	}
}