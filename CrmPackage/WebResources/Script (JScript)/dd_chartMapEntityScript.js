function onLoad() {

    if (Xrm.Page.getAttribute("dd_chartname").getValue()) {
        Xrm.Page.getControl("dd_chartname").setDisabled(true);
        Xrm.Page.getControl("dd_entity").setDisabled(true);
        Xrm.Page.getControl("dd_chartdescription").setDisabled(true);
    }

    maptype_onChange();
}

function onSave() {

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
        }
        else {
            Xrm.Page.getControl("dd_intensitycalculation").setDisabled(false);
        }
    }
}