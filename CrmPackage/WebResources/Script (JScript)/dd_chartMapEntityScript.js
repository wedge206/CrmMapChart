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
	dd_CRMService.RetrieveMultiple(
					"organization",
					"$select=organizationid,bingmapsapikey&$top=1",  // TODO: Check casing
					function (results) {  //successCallback
						var oldKey = results[0].bingmapsapikey || "";
						var newKey = prompt("Enter your Bing Maps API Key:\nThis can also be set in the System Settings", oldKey);
						if (newKey != oldKey) {
							Xrm.Utility.confirmDialog(
								"This will update the saved key to: " + newKey + "\nAre you Sure?",
								function () {
									dd_CRMService.Update(
										results[0].organizationid,
										{ "bingmapsapikey": newKey },
										"organization",
										function () { Xrm.Utility.Alert("Key Saved Successfully") },
										function (error) { Xrm.Utility.Alert(error); }
									);
								}
							);
						}
					},
					function (error) { //errorCallback
						Xrm.Utility.Alert(error);
					}
				);
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