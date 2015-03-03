// TODO: replace almost all of this with business rules
function onLoad() {

	if (Xrm.Page.getAttribute("dd_chartname").getValue()) {
		Xrm.Page.getControl("dd_chartname").setDisabled(true);
		Xrm.Page.getControl("dd_entity").setDisabled(true);
		Xrm.Page.getControl("dd_chartdescription").setDisabled(true);
	}
	if (Xrm.Page.getAttribute("dd_enablecaching").getValue() == null) {
		Xrm.Page.getAttribute("dd_enablecaching").setValue(false);
	}

	maptype_onChange();
	enableCaching_onChange();
}

function onSave() {

}

function setBingMapsKey() {  // Called from the ribbon button
	RetrieveApiKey(function (org) {
		var oldKey = org.BingMapsApiKey || "";
		var newKey = prompt("Enter your Bing Maps API Key:", oldKey);  // Would really like to have an Xrm.Utility.Prompt() ...
		if (newKey != null && newKey !== oldKey) {
			Xrm.Utility.confirmDialog(
				"This will update the saved key from: " + oldKey + " to: " + newKey + "\n\n  Are you Sure?",
				function () {
					UpdateApiKey(org.OrganizationId, newKey, function () { Xrm.Utility.Alert("Key Saved Successfully.") });
				}
			);
		}
	});
}

function SetApiKeyDisplayRule() {
	debugger;
	return true;
	// This button only applies to CRM Online.  OnPrem users should use the System Settings page.
	//return Xrm.Page.context.getClientUrl().indexOf("crm.dynamics.com") > -1;
}

function enableCaching_onChange() {
	if (Xrm.Page.getAttribute("dd_enablecaching").getValue() == true) {
		Xrm.Page.getAttribute("dd_latitudefield").setRequiredLevel("required");
		Xrm.Page.getAttribute("dd_longitudefield").setRequiredLevel("required");
	}
	else {
		Xrm.Page.getAttribute("dd_latitudefield").setRequiredLevel("none");
		Xrm.Page.getAttribute("dd_longitudefield").setRequiredLevel("none");
	}
}

function maptype_onChange() {

	var mapType = Xrm.Page.getAttribute("dd_maptype").getValue();
	var intensityType = Xrm.Page.getAttribute("dd_heatmapbasedon").getValue();
	var standard, heatmap, fixed, calculated;

	if (mapType == 1) {
		standard = true;
		heatmap = false;
	}
	else if (mapType == 2) {
		standard = false;
		heatmap = true;

		if (intensityType == 1) {
			fixed = false;
			calculated = false;
		}
		else if (intensityType == 2) {
			fixed = false;
			calculated = true;

			if (Xrm.Page.getAttribute("dd_intensityrange").getValue() == 2) {
				fixed = true;
			}
			else {
				fixed = false;
			}
		}
	}

	Xrm.Page.ui.tabs.get("general").sections.get("pinsettings").setVisible(standard);
	Xrm.Page.ui.tabs.get("general").sections.get("pinsettings").controls.forEach(
        function (control, index) {
        	control.setDisabled(!standard);
        });

	Xrm.Page.ui.tabs.get("general").sections.get("heatmap").setVisible(heatmap);
	Xrm.Page.ui.tabs.get("general").sections.get("heatmap").controls.forEach(
        function (control, index) {
        	control.setDisabled(!heatmap);
        });

	Xrm.Page.ui.tabs.get("general").sections.get("heatmap2").setVisible(heatmap && calculated);
	Xrm.Page.ui.tabs.get("general").sections.get("heatmap2").controls.forEach(
        function (control, index) {
        	control.setDisabled(!(heatmap && calculated));
        });

	Xrm.Page.ui.tabs.get("general").sections.get("heatmap3").setVisible(heatmap && fixed);
	Xrm.Page.ui.tabs.get("general").sections.get("heatmap4").controls.forEach(
        function (control, index) {
        	control.setDisabled(!(heatmap && fixed));
        });

	Xrm.Page.ui.tabs.get("general").sections.get("heatmap4").setVisible(heatmap);
	Xrm.Page.ui.tabs.get("general").sections.get("heatmap4").controls.forEach(
        function (control, index) {
        	control.setDisabled(!heatmap);
        });

	if (intensityType == 1) {
		Xrm.Page.getControl("dd_intensity").setDisabled(false);
		Xrm.Page.getControl("dd_numericfield").setDisabled(true);
	}
	else if (intensityType == 2) {
		Xrm.Page.getControl("dd_intensity").setDisabled(true);
		Xrm.Page.getControl("dd_numericfield").setDisabled(false);

		if (Xrm.Page.getAttribute("dd_intensityrange").getValue() == 2) {
			Xrm.Page.getControl("dd_intensitycalculation").setDisabled(true);
			Xrm.Page.getControl("dd_deviations").setDisabled(true);
		}
		else {
			Xrm.Page.getControl("dd_intensitycalculation").setDisabled(false);

			if (Xrm.Page.getAttribute("dd_intensitycalculation").getValue() == 2) {
				Xrm.Page.getControl("dd_deviations").setDisabled(false);
			}
			else {
				Xrm.Page.getControl("dd_deviations").setDisabled(true);
			}
		}
	}
}

function RetrieveApiKey(callback) {
	var req = new XMLHttpRequest();
	req.open("GET", Xrm.Page.context.getClientUrl() + "/XRMServices/2011/OrganizationData.svc/OrganizationSet?$select=OrganizationId,BingMapsApiKey&$filter=Name eq '" + Xrm.Page.context.getOrgUniqueName() + "'&$top=1", true);
	req.setRequestHeader("Accept", "application/json");
	req.setRequestHeader("Content-Type", "application/json; charset=utf-8");
	req.onreadystatechange = function () {
		if (this.readyState == 4 /* complete */) {
			req.onreadystatechange = null;
			if (this.status == 200) {
				callback(JSON.parse(this.responseText).d.results[0]);
			}
		}
		else {
			throw "Failed to retrieve existing Api Key";
		}
	};
	req.send();
}

function UpdateApiKey(id, newKey, successCallback) {
	var req = new XMLHttpRequest();
	req.open("POST", encodeURI(Xrm.Page.context.getClientUrl() + "/XRMServices/2011/OrganizationData.svc/OrganizationSet(guid'" + id + "')"), true);
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
				throw "Failed to update Api Key";
			}
		}
	};
	req.send(JSON.stringify({ BingMapsApiKey: newKey }));
}