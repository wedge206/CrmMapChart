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
    var standard, heatmap;

    if (mapType == 1) {
        standard = true;
        heatmap = false;
    }
    else if (mapType == 2) {
        standard = false;
        heatmap = true;
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
}