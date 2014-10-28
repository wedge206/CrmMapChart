var XMLParser = XMLParser || {  // XML Parser functions
        ProcessSoapResponse: function (responseXml, Callback) {
            try {
                var namespaces = [
                    "xmlns:s='http://schemas.xmlsoap.org/soap/envelope/'",
                    "xmlns:a='http://schemas.microsoft.com/xrm/2011/Contracts'",
                    "xmlns:i='http://www.w3.org/2001/XMLSchema-instance'",
                    "xmlns:b='http://schemas.microsoft.com/crm/2011/Contracts'",
                    "xmlns:c='http://schemas.datacontract.org/2004/07/System.Collections.Generic'"];
                responseXml.setProperty("SelectionNamespaces", namespaces.join(" "));
            } catch (e) { }

            var resultNodes = this._selectNodes(responseXml, "//a:Results/a:KeyValuePairOfstringanyType");
            Callback(this.ObjectifyNodes(resultNodes));
        },
        ProcessSoapError: function (responseXml, Callback) {
            try {
                var namespaces = [
                    "xmlns:s='http://schemas.xmlsoap.org/soap/envelope/'",
                    "xmlns:a='http://schemas.microsoft.com/xrm/2011/Contracts'",
                    "xmlns:i='http://www.w3.org/2001/XMLSchema-instance'",
                    "xmlns:b='http://schemas.microsoft.com/crm/2011/Contracts'",
                    "xmlns:c='http://schemas.datacontract.org/2004/07/System.Collections.Generic'"];
                responseXml.setProperty("SelectionNamespaces", namespaces.join(" "));
            } catch (e) { }

            var errorNode = this._selectSingleNode(responseXml, "//s:Fault/faultstring");
            Callback(Error(this._getNodeText(errorNode)));
        },
        ObjectifyNodes: function (nodes) {
            var result = {};

            for (var i = 0; i < nodes.length; i++) {
                var fieldName = this._getNodeText(nodes[i].firstChild);
                var fieldValue = nodes[i].childNodes[1];
                result[fieldName] = this.ObjectifyNode(fieldValue);
            }

            return result;
        },
        ObjectifyNode: function (node) {
            if (node.attributes != null) {
                if (node.attributes.getNamedItem("i:nil") != null && node.attributes.getNamedItem("i:nil").nodeValue == "true") {
                    return null;
                }

                var nodeTypeName = node.attributes.getNamedItem("i:type") == null ? "c:string" : node.attributes.getNamedItem("i:type").nodeValue;

                switch (nodeTypeName) {
                    case "a:EntityReference":
                        return {
                            id: this._getNodeText(node.childNodes[0]),
                            entityType: this._getNodeText(node.childNodes[1])
                        };
                	case "a:AliasedValue":
                		return this._getNodeText(node.childNodes[2]);
                	case "a:Entity":
                        return this.ObjectifyRecord(node);
                    case "a:EntityCollection":
                    	return this.ObjectifyCollection(node.firstChild);
                    case "c:dateTime":
                        return this.ParseIsoDate(this._getNodeText(node));
                    case "c:guid":
                    case "c:string":
                        return this._getNodeText(node);
                    case "c:int":
                        return parseInt(this._getNodeText(node));
                    case "a:OptionSetValue":
                        return parseInt(this._getNodeText(node.childNodes[0]));
                    case "c:boolean":
                        return this._getNodeText(node.childNodes[0]) == "true";
                    case "c:double":
                    case "c:decimal":
                    case "a:Money":
                        return parseFloat(this._getNodeText(node.childNodes[0]));
                    default:
                        return null;
                }
            }

            return null;
        },
        ObjectifyCollection: function (node) {
            var result = [];
            for (var i = 0; i < node.childNodes.length; i++) {
                result.push(this.ObjectifyRecord(node.childNodes[i]));
            }

            return result;
        },
        ObjectifyRecord: function (node) {
            var result = {};

            result.logicalName = (node.childNodes[4].text !== undefined) ? node.childNodes[4].text : node.childNodes[4].textContent;
            result.id = (node.childNodes[3].text !== undefined) ? node.childNodes[3].text : node.childNodes[3].textContent;

            result.attributes = this.ObjectifyNodes(node.childNodes[0].childNodes);
            result.formattedValues = this.ObjectifyNodes(node.childNodes[2].childNodes);

            return result;
        },
        ParseIsoDate: function (s) {
            if (s == null || !s.match(this.isoDateExpression))
                return null;

            var dateParts = this.isoDateExpression.exec(s);
            return new Date(Date.UTC(parseInt(dateParts[1], 10),
                parseInt(dateParts[2], 10) - 1,
                parseInt(dateParts[3], 10),
                parseInt(dateParts[4], 10) - (dateParts[8] == "" || dateParts[8] == "Z" ? 0 : parseInt(dateParts[8])),
                parseInt(dateParts[5], 10),
                parseInt(dateParts[6], 10)));
        },
        _selectNodes: function (node, xPathExpression) {
            if (typeof (node.selectNodes) != "undefined") {
                return node.selectNodes(xPathExpression);
            }
            else {
                var output = [];
                var xPathResults = node.evaluate(xPathExpression, node, this._NSResolver, XPathResult.ANY_TYPE, null);
                var result = xPathResults.iterateNext();
                while (result) {
                    output.push(result);
                    result = xPathResults.iterateNext();
                }
                return output;
            }
        },
        _selectSingleNode: function (node, xpathExpr) {
            if (typeof (node.selectSingleNode) != "undefined") {
                return node.selectSingleNode(xpathExpr);
            }
            else {
                var xpe = new XPathEvaluator();
                var xPathNode = xpe.evaluate(xpathExpr, node, this._NSResolver, XPathResult.FIRST_ORDERED_NODE_TYPE, null);
                return (xPathNode != null) ? xPathNode.singleNodeValue : null;
            }
        },
        _getNodeText: function (node) {
            if (typeof (node.text) != "undefined") {
                return node.text;
            }
            else {
                return node.textContent;
            }
        },
        _isNodeNull: function (node) {
            if (node == null) {
                return true;
            }

            if ((node.attributes.getNamedItem("i:nil") != null) && (node.attributes.getNamedItem("i:nil").value == "true")) {
                return true;
            }
            return false;
        },
        _getNodeName: function (node) {
            if (typeof (node.baseName) != "undefined") {
                return node.baseName;
            }
            else {
                return node.localName;
            }
        },
        _NSResolver: function (prefix) {
            var ns = {
                "s": "http://schemas.xmlsoap.org/soap/envelope/",
                "a": "http://schemas.microsoft.com/xrm/2011/Contracts",
                "i": "http://www.w3.org/2001/XMLSchema-instance",
                "b": "http://schemas.microsoft.com/crm/2011/Contracts",
                "c": "http://schemas.datacontract.org/2004/07/System.Collections.Generic"
            };
            return ns[prefix] || null;
        },
        isoDateExpression: /^(\d{4})-(\d{2})-(\d{2})T(\d{2}):(\d{2}):(\d{2})\.?(\d*)?(Z|[+-]\d{2}?(:\d{2})?)?$/,
        __namespace: true
}
