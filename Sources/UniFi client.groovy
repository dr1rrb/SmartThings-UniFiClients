/**
 *  UniFi client device
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
metadata {
	definition (
		name: "UniFi client device", 
		namespace: "torick.net", 
		author: "Dr1rrb") 
	{
		capability "Presence Sensor"
		capability "Sensor"
		capability "Refresh"
        capability "Actuator"
	}

	simulator 
	{
		// TODO: define status and reply messages here
	}

	tiles 
	{
		standardTile("presence", "device.presence", width: 3, height: 2, canChangeBackground: true, canChangeIcon: true) {
			state("not present", label:'not present', icon:"st.presence.tile.not-present", backgroundColor:"#ffffff", action: "refresh", nextState: "not present")
			state("present", label:'present', icon:"st.presence.tile.present", backgroundColor:"#53a7c0", action: "refresh", nextState: "present")
		}
		main "presence"
		details "presence"
	}
}

def installed() 
{
	log.debug "Installed with settings: ${settings}"

    //updateDataValue("host", "192.168.144.38:5000")

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
	def oldHost = getDataValue("host")
	if (newHost && oldHost != newHost) 
	{
		log.debug "Host updated: ${newHost} (was: ${oldHost})"

		updateDataValue("host", newHost);
        subscribeToController();
	}
}

def configure() 
{
	unschedule();
	//unsubscribe();

    runEvery3Hours("subscribeToController");
	subscribeToController();
}

def subscribeToController() 
{
	log.debug "Subscribing to controller"

	def host = getDataValue("host");
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
    
    log.debug "Sent ${command} => ${result}"
}

// parse events into attributes
def parse(String description)
{
	log.debug "Received a message from the hub '${description}'"
	
    def msg = parseLanMessage(description)

	if (msg?.json?.presence)
    {
		def previous = device.currentValue("presence");
		def changed = msg.json.presence != previous;
        
        log.debug "Received a presence state changed notification '${msg.json.presence}' (was ${previous}; changed: ${changed})"
        
		sendEvent(
        	name: "presence", 
            value: msg.json.presence,
            descriptionText: "${device.displayName} is ${msg.json.presence}",
            displayed: changed,
            isStateChange: changed);
    }
}
