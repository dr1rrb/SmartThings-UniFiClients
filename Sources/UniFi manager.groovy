/**
 *  UniFi client devices manager
 *
 *  Copyright 2017 Dr1rrb
 *
 *  Licensed under the Apache License, Version 2.0 (the "License"); you may not use this file except
 *  in compliance with the License. You may obtain a copy of the License at:
 *
 *      http://www.apache.org/licenses/LICENSE-2.0
 *
 *  Unless required by applicable law or agreed to in writing, software distributed under the License is distributed
 *  on an "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied. See the License
 *  for the specific language governing permissions and limitations under the License.
 *
 */
definition(
	name: "UniFi client devices manager",
	namespace: "torick.net",
	author: "Dr1rrb",
	description: "Use clients of your unifi network as presence sensor.",
	category: "Mode Magic",
    singleInstance: true,
	iconUrl: "https://upload.wikimedia.org/wikipedia/commons/7/71/Ubiquiti_Logo.png",
	iconX2Url: "https://upload.wikimedia.org/wikipedia/commons/7/71/Ubiquiti_Logo.png",
	iconX3Url: "https://upload.wikimedia.org/wikipedia/commons/7/71/Ubiquiti_Logo.png")


preferences 
{
	//	state.devices = [:]; // erase the previous state of discovery
	page(name: "searchTargetSelection", title: "UniFi controller setup", nextPage: "deviceDiscovery", uninstall: true) {
		section("Download and run the SmartThings UniFi client presence application, then tap 'Next' to start the discovery.") {
			//input "searchTarget", "string", title: "Search Target", defaultValue: "urn:torick-net:device:WiFiDevice:1", required: true
		}
	}
	page(name: "deviceDiscovery", title: "UniFi client setup", content: "deviceDiscovery")
}

def deviceDiscovery() 
{
	// First, start the SSDP discovery for setup. This mode runs only once, the "recurrancy" is ensured by the auto-refresh of this page.
	startSsdpDiscoveryForSetup()

	def devices = getDevices().collectEntries { key, value -> [(key): "${value.name}"] }

	return dynamicPage(name: "deviceDiscovery", title: "Discovery Started!", nextPage: "", refreshInterval: 5, install: true, uninstall: false) {
		section("Please wait while we discover your UniFi clients. Discovery can take a while, so sit back and relax! Select your device below once discovered.") {
			input "selectedDevices", "enum", required: false, title: "Select clients (${devices.size() ?: 0} found)", multiple: true, options: devices
		}
	}
}

def installed() 
{
	log.debug "Installed with settings: ${settings}"

	initialize()
}

def updated() 
{
	log.debug "Updated with settings: ${settings}"

	initialize()
}

def initialize() 
{
	startSsdpDiscoveryForMaintenance()

	setupSelectedDevices()
}

// Region: SSDP discovery
void startSsdpDiscoveryForMaintenance()
{
	unschedule();
	unsubscribe();

	subscribe(location, "ssdpTerm.urn:torick-net:device:UniFiDevice:1", onDeviceDiscoveredForMaintenance);

	requestSsdpDiscovery();
	runEvery15Minutes("requestSsdpDiscovery");
}

void startSsdpDiscoveryForSetup()
{
	unschedule();
	unsubscribe();

	subscribe(location, "ssdpTerm.urn:torick-net:device:UniFiDevice:1", onDeviceDiscoveredForSetup);

	requestSsdpDiscovery();
}

void requestSsdpDiscovery() {
	log.debug "Searching for UniFi devices on LAN using SSDP"

	sendHubCommand(new physicalgraph.device.HubAction("lan discovery urn:torick-net:device:UniFiDevice:1", physicalgraph.device.Protocol.LAN))
}


def onDeviceDiscoveredForMaintenance(evt) {
	def eventArgs = parseLanMessage(evt.description);
	def id = toDeviceId(eventArgs.ssdpUSN.toString());
	def host = toHost(eventArgs.networkAddress, eventArgs.deviceAddress);
    def mac = "${eventArgs.mac}";

    log.debug "Received SSDP for maintenance (host: ${host}; device: '${id}'; controller: ${mac})"

	// Update the child device, if exists
    def client = getChild(id);
    if (client)
    {
    	log.debug "Client device '${id}' exists (${client}), set its host to ${host}"
    	client.updateHost(host);
    }
    
    // Also update the controller if exists, or create it if missing
    def controller = getChild(mac);
    if (controller)
    {
    	log.debug "Controller for '${mac}' exists (${controller}), set its host to ${host}"
    	controller.updateHost(host);
    }
    else if (client)
    {
    	log.debug "Controller is missing for '${mac}', create a new one"
    	ensureHub(host, mac, client.hub.id)
    }
}

def onDeviceDiscoveredForSetup(evt) {
	def eventArgs = parseLanMessage(evt.description);
	def id = toDeviceId(eventArgs.ssdpUSN.toString());
	def host = toHost(eventArgs.networkAddress, eventArgs.deviceAddress);
	def path = eventArgs.ssdpPath

    log.debug "Received SSDP for setup: '${id}', requesting its description document on: ${host}${path} (${evt.description})";

	if (getChild(id))
	{
		log.debug "Device '${id}' is already installed";
		return;
	}

	// Confirm the device by requesting its description document
	sendHubCommand(new physicalgraph.device.HubAction(
		"""GET ${path} HTTP/1.1\r\nHOST: ${host}\r\n\r\n""", 
		physicalgraph.device.Protocol.LAN, 
		host, 
		[callback: onDeviceConfirmed]))
}

void onDeviceConfirmed(physicalgraph.device.HubResponse response) 
{
	if(response.status != 200)
	{
		log.error "Failed to get description document (${response})";
		return;
	}

	def document = response.xml;
	def id = toDeviceId(document?.device.UDN.text());
    def host = toHost(response.ip, response.port);

	if (!id)
	{
		log.error "Invalid device id"
		return;
	}

	// physicalgraph.device.HubResponse(index:18, mac:485B3986D3F3, ip:C0A89026, port:1388, requestId:7feb3bd6-35d5-4f04-ac36-171b6ffa65a7, hubId:df568065-1b38-4ac7-843a-cd044d4e1a6d, callback:deviceDescriptionHandler)

	def device = [
			id: id,
            hub: location.hubs[0].id,
			host: host,
            hostMac: response.mac,
			name: document.device.friendlyName.text(), 
			model:document.device.modelName?.text(), 
			serialNumber:document.device.serialNum?.text(), 
		];
    
    log.debug "Received device description for '${id}': ${device}."
    
	getDevices()[id] = device;
}

// Region: device setup
def setupSelectedDevices() {
	def devices = getDevices();
	
    if(!selectedDevices)
    {
    	log.debug "No devices selected yet."
    }
	else if (selectedDevices instanceof String) // The magic of a non strongly typed language ...
	{
		log.debug "Selected devices (1) : ${selectedDevices}"

		setupDevice(selectedDevices);
	}
	else
	{
		log.debug "Selected devices (${selectedDevices.size()}) : ${selectedDevices.collect{ it -> it }}"

		selectedDevices.each { key -> setupDevice(key) }
	}
}

def setupDevice(id)
{
	def selectedDevice = devices[id]
	if (selectedDevice == null)
	{
		log.error "A device was selected but it is missing from the discovered devices: ${id}";
		return;
	}
        
	def child = getChild(id)
	if (child) 
	{
		log.debug "Device ${id} already exists"
	}
	else
	{
    	// First, ensure to create a hub to act as callback multiplexer
    	ensureHub(selectedDevice.host, selectedDevice.hostMac, selectedDevice.hub);
    
		log.debug "Creating UniFi client device ${id}"
        addChildDevice(
			"torick.net", 
			"UniFi client device",
			selectedDevice.id, // == id, 
            selectedDevice.hub, 
			[
                "label": selectedDevice.name,
                "data": [
                    "id": selectedDevice.id,
					"name": selectedDevice.name,
					"host": selectedDevice.host,
                    "type": "device"
                ]
            ])
    }
}

def ensureHub(host, mac, hub)
{
    def controller = getChild(mac);
    if (controller == null)
    {
    	log.debug "Creating controller '${mac}' on hub '${hub}' (host: ${host})"
    
        addChildDevice(
            "torick.net", 
            "UniFi controller",
            mac,
            hub, 
            [
                "label": "UniFi controller",
                "data": [
                    "host": host,
                    "hostMac": mac,
                    "type": "hub"
                ]
            ])
    }
}

// Region: Helpers
def getChild(id) { getChildDevice(id) }

def getDevices() { state.devices ?: (state.devices = [:]); }

def toDeviceId(ssdpUSN) { ssdpUSN.split(':')[1][0..-1]; }

//def toHost(ip, port) { "${toIP(ip)}:${toInt(port)}"; }
def toHost(ip, port) { "${toIP(ip)}:5000"; } // As of 2017-02, the parsing of the port is not working. Hard code it to 5000 for now.

Integer toInt(hex) { Integer.parseInt(hex,16) }

String toIP(hex) { [toInt(hex[0..1]),toInt(hex[2..3]),toInt(hex[4..5]),toInt(hex[6..7])].join(".") }