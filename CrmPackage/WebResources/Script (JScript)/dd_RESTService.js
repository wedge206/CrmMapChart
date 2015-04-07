var dd_CRMService = (function (dd_CRMService) {
	function _ODataPath() {
		return Xrm.Page.context.getClientUrl() + "/XRMServices/2011/OrganizationData.svc/";
	}
	function _dateReviver(key, value) {
		if (typeof value === 'string') {
			if (/Date\(([-+]?\d+)\)/.exec(value)) {
				return new Date(parseInt(value.replace("/Date(", "").replace(")/", ""), 10));
			}
		}
		return value;
	}
	function _encodeXml(strInput) {
		var div = document.createElement('div');
		div.appendChild(document.createTextNode(strInput));
		return div.innerHTML;
	}
	function _errorHandler(req) {
		if (req.status == 12029) {
			return new Error("The attempt to connect to the server failed.");
		}
		if (req.status == 12007) {
			return new Error("The server name could not be resolved.");
		}

		var errorText;
		try {
			errorText = JSON.parse(req.responseText).error.message.value;
		}
		catch (e) {
			errorText = req.responseText;
		}

		return new Error("Error : " +
			  req.status + ": " +
			  req.statusText + ": " + errorText);
	}
	dd_CRMService.Create = function Create(object, type, successCallback, errorCallback) {
		var async = !!successCallback;
		successCallback = successCallback || function (result) { return result; };
		errorCallback = errorCallback || function (result) { return result; };

		var req = new XMLHttpRequest();
		req.open("POST", encodeURI(_ODataPath() + type + "Set"), async);
		req.setRequestHeader("Accept", "application/json");
		req.setRequestHeader("Content-Type", "application/json; charset=utf-8");
		req.onreadystatechange = function () {
			if (this.readyState == 4 /* complete */) {
				req.onreadystatechange = null;
				if (this.status == 201) {
					return successCallback(JSON.parse(this.responseText, _dateReviver).d);
				}
				else {
					return errorCallback(_errorHandler(this));
				}
			}
		};
		req.send(JSON.stringify(object));
	}
	dd_CRMService.Retrieve = function Retrieve(id, type, select, expand, successCallback, errorCallback) {
		var async = !!successCallback;
		successCallback = successCallback || function (result) { return result; };
		errorCallback = errorCallback || function (result) { return result; };

		var systemQueryOptions = "";

		if (select != null || expand != null) {
			systemQueryOptions = "?";
			if (select != null) {
				var selectString = "$select=" + select;
				if (expand != null) {
					selectString = selectString + "," + expand;
				}
				systemQueryOptions = systemQueryOptions + selectString;
			}
			if (expand != null) {
				systemQueryOptions = systemQueryOptions + "&$expand=" + expand;
			}
		}

		var req = new XMLHttpRequest();
		req.open("GET", encodeURI(_ODataPath() + type + "Set(guid'" + id + "')" + systemQueryOptions), async);
		req.setRequestHeader("Accept", "application/json");
		req.setRequestHeader("Content-Type", "application/json; charset=utf-8");
		req.onreadystatechange = function () {
			if (this.readyState == 4 /* complete */) {
				req.onreadystatechange = null;
				if (this.status == 200) {
					return successCallback(JSON.parse(this.responseText, _dateReviver).d);
				}
				else {
					return errorCallback(_errorHandler(this));
				}
			}
		};
		req.send();
	}
	dd_CRMService.Update = function Update(id, object, type, successCallback, errorCallback) {
		var async = !!successCallback;
		successCallback = successCallback || function (result) { return result; };
		errorCallback = errorCallback || function (result) { return result; };

		var req = new XMLHttpRequest();
		req.open("POST", encodeURI(_ODataPath() + type + "Set(guid'" + id + "')"), async);
		req.setRequestHeader("Accept", "application/json");
		req.setRequestHeader("Content-Type", "application/json; charset=utf-8");
		req.setRequestHeader("X-HTTP-Method", "MERGE");
		req.onreadystatechange = function () {
			if (this.readyState == 4 /* complete */) {
				req.onreadystatechange = null;
				if (this.status == 204 || this.status == 1223) {
					return successCallback();
				}
				else {
					return errorCallback(_errorHandler(this));
				}
			}
		};
		req.send(JSON.stringify(object));
	}
	dd_CRMService.Delete = function Delete(id, type, successCallback, errorCallback) {
		var async = !!successCallback;
		successCallback = successCallback || function (result) { return result; };
		errorCallback = errorCallback || function (result) { return result; };

		var req = new XMLHttpRequest();
		req.open("POST", encodeURI(_ODataPath() + type + "Set(guid'" + id + "')"), async);
		req.setRequestHeader("Accept", "application/json");
		req.setRequestHeader("Content-Type", "application/json; charset=utf-8");
		req.setRequestHeader("X-HTTP-Method", "DELETE");
		req.onreadystatechange = function () {
			if (this.readyState == 4 /* complete */) {
				req.onreadystatechange = null;
				if (this.status == 204 || this.status == 1223) {
					return successCallback();
				}
				else {
					return errorCallback(_errorHandler(this));
				}
			}
		};
		req.send();
	}
	dd_CRMService.RetrieveMultiple = function RetrieveMultiple(type, options, successCallback, errorCallback) {
		var async = !!successCallback;
		successCallback = successCallback || function (result) { return result; };
		errorCallback = errorCallback || function (result) { return result; };

		var optionsString;
		if (options != null) {
			if (options.charAt(0) != "?") {
				optionsString = "?" + options;
			}
			else { optionsString = options; }
		}
		var results = [];

		var req = new XMLHttpRequest();
		req.open("GET", _ODataPath() + type + "Set" + optionsString, async);
		req.setRequestHeader("Accept", "application/json");
		req.setRequestHeader("Content-Type", "application/json; charset=utf-8");
		req.onreadystatechange = function () {
			if (this.readyState == 4 /* complete */) {
				req.onreadystatechange = null;
				if (this.status == 200) {
					var returned = JSON.parse(this.responseText, _dateReviver).d;
					results = results.concat(returned.results);
					if (returned.__next != null) {
						var queryOptions = returned.__next.substring((_ODataPath() + type + "Set").length);
						return RetrieveMultiple(type, queryOptions, function (result) { return result; });
					}
					else {
						return successCallback(results);
					}
				}
				else {
					return errorCallback(_errorHandler(this));
				}
			}
		};
		req.send();
	}
	dd_CRMService.RetrieveFetchXml = function RetrieveFetchXml(requestXml, successCallback, errorCallback) {
		var async = !!successCallback;
		successCallback = successCallback || function (result) { return result; };
		errorCallback = errorCallback || function (result) { return result; };

		var req = new XMLHttpRequest();
		req.open("POST", Xrm.Page.context.getClientUrl() + "/XRMServices/2011/Organization.svc/web", async);
		try {
			req.responseType = 'msxml-document';
		} catch (e) { }
		req.setRequestHeader("Accept", "application/xml, text/xml, */*");
		req.setRequestHeader("Content-Type", "text/xml; charset=utf-8");
		req.setRequestHeader("SOAPAction", "http://schemas.microsoft.com/xrm/2011/Contracts/Services/IOrganizationService/RetrieveMultiple");
		req.onreadystatechange = function () {
			if (req.readyState == 4) {
				if (req.status == 200) {
					return XMLParser.ProcessSoapResponse(req.responseXML, successCallback);
				}
				else {
					return XMLParser.ProcessSoapError(req.responseXML, errorCallback);
				}
			}
		};

		req.send("<s:Envelope xmlns:s='http://schemas.xmlsoap.org/soap/envelope/'> \
					<s:Body> \
					  <RetrieveMultiple xmlns='http://schemas.microsoft.com/xrm/2011/Contracts/Services' xmlns:i='http://www.w3.org/2001/XMLSchema-instance'> \
						<query i:type='a:FetchExpression' xmlns:a='http://schemas.microsoft.com/xrm/2011/Contracts'> \
						  <a:Query>" + _encodeXml(requestXml) + "</a:Query> \
						</query> \
					  </RetrieveMultiple> \
					</s:Body> \
			 	  </s:Envelope>");
	}

	return dd_CRMService;
}(dd_CRMService || {}));
