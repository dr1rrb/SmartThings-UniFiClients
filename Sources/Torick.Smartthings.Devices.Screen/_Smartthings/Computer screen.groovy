/**
 *  Computer screen
 *
 *  Copyright 2017 Dr1rrb
 *
 *  Licensed under the Apache License, Version 2.0 (the "License"); you may not use this file except
 *  in compliance with the License. You may obtain a copy of the License at:
 *
 *	  http://www.apache.org/licenses/LICENSE-2.0
 *
 *  Unless required by applicable law or agreed to in writing, software distributed under the License is distributed
 *  on an "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied. See the License
 *  for the specific language governing permissions and limitations under the License.
 *
 */
metadata {
	definition (
		name: "Computer screen", 
		namespace: "torick.net", 
		author: "Dr1rrb") 
	{
		capability "Switch"
		capability "Sensor"
		capability "Refresh"
		capability "Actuator"
		
		attribute "switch", "string"
		attribute "host", "string"
	}

	simulator 
	{
		// TODO: define status and reply messages here
	}
	
	tiles
	{
		multiAttributeTile(name:"switch", type: "lighting", width: 3, height: 2, canChangeIcon: true){
			tileAttribute ("device.switch", key: "PRIMARY_CONTROL") {
				attributeState "on", label: '${name}', action: "switch.off", icon: "st.switches.switch.on", backgroundColor: "#00A0DC", nextState:"off"
				attributeState "off", label: '${name}', action: "switch.on", icon: "st.switches.switch.off", backgroundColor: "#ffffff", nextState:"on"
			}
		}

		standardTile("refresh", "device.switch", width: 2, height: 2, inactiveLabel: false, decoration: "flat") {
			state "default", label:'', action:"refresh.refresh", icon:"st.secondary.refresh"
		}

		main "switch"
		details(["switch","refresh"])
	}
}

def installed() 
{
	log.debug "Installed with settings: ${settings}"

	configure()
}

def updated() 
{
	log.debug "Updated with settings: ${settings}"

	configure()
}

def refresh()
{
	log.debug "Refresh requested"

	configure()
}

def updateHost(newHost) 
{
	def oldHost = device.currentValue("host");
	if (newHost && oldHost != newHost) 
	{
		log.debug "Host updated: ${newHost} (was: ${oldHost})"

		sendEvent(
			name: "host", 
			value: newHost,
			descriptionText: "Host ${device.displayName} was updated. It's now ${newHost}",
			displayed: false,
			isStateChange: true);

		subscribeToController();
	}
}

def updateHostStatus(isOnline)
{
	def previous = device.currentValue("presence");
	log.debug "Received notification of host status isOnline: ${isOnline} (Device was ${previous})."

	if(isOnline)
	{
		refresh();
	}
	else
	{
		def changed = previous == "present";
		sendEvent(
			name: "presence", 
			value: "not present",
			descriptionText: "${device.displayName} is not present",
			displayed: changed,
			isStateChange: changed);
	}
}

def configure() 
{
	unschedule();

	runEvery3Hours("subscribeToController");
	subscribeToController();
}

def subscribeToController() 
{
	log.debug "Subscribing to controller"

	def host = device.currentValue("host");
	def id = device.deviceNetworkId;
	def callback = "http://${device.hub.getDataValue("localIP")}:${device.hub.getDataValue("localSrvPortTCP")}/api/device/${id}";
	def command = new physicalgraph.device.HubAction(
		method: "SUBSCRIBE",
		path: "/api/device/${id}",
		headers: [
			Host: host,
			CALLBACK: "<${callback}>",
			NT: "upnp:event",
			TIMEOUT: "Second-28800",
			"Smartthings-Device": id
		]
	);
	
	def result = sendHubCommand(command)
	
	log.debug "Sent to ${host} ${command} => ${result}"
}

// *********************** Device specific
def on()
{
	log.debug "Turning screen on"

	def host = device.currentValue("host");
	def id = device.deviceNetworkId;
	def command = new physicalgraph.device.HubAction(
		method: "PUT",
		path: "/api/screen/on",
		headers: [
			Host: host,
			"Smartthings-Device": id,
            "Content-Length": 0
		]
	);
	
	def result = sendHubCommand(command)
	
	log.debug "Sent to ${host} ${command} => ${result}"
}

def off()
{
	log.debug "Turning screen off"

	def host = device.currentValue("host");
	def id = device.deviceNetworkId;
	def command = new physicalgraph.device.HubAction(
		method: "PUT",
		path: "/api/screen/off",
		headers: [
			Host: host,
			"Smartthings-Device": id,
            "Content-Length": 0
		]
	);
	
	def result = sendHubCommand(command)
	
	log.debug "Sent to ${host} ${command} => ${result}"
}

// parse events into attributes
def parse(String description)
{
	//log.debug "Received a message from the hub '${description}'"
	
	def msg = parseLanMessage(description)
	if (msg?.json?.status)
	{
		def previous = device.currentValue("switch");
		def changed = msg.json.status != previous;
		
		log.debug "Received a presence state notification '${msg.json.status}' (was '${previous}'; changed: ${changed})"
		
		sendEvent(
			name: "switch", 
			value: msg.json.status,
			descriptionText: "${device.displayName} is ${msg.json.status}",
			displayed: changed,
			isStateChange: changed);
	}
}
