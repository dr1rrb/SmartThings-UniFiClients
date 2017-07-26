import groovy.time.TimeCategory

/**
 *  UniFi controller
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
	definition (name: "UniFi controller", namespace: "torick.net", author: "Dr1rrb") {
		capability "Refresh"
        
        attribute "multiplexerStatus", "enum", ["offline", "online"]
	}

	simulator {
		// TODO: define status and reply messages here
	}

	tiles {
		standardTile("status", "device.multiplexerStatus", width: 3, height: 2, canChangeBackground: true, canChangeIcon: true) {
            state "offline", label:'Inactive', icon:"st.unknown.zwave.static-controller", backgroundColor:"#ffffff", action: "refresh", nextState: "offline"
			state "online", label:'Active', icon:"st.unknown.zwave.static-controller", backgroundColor:"#53a7c0", action: "refresh", nextState: "online"
		}
    
    	valueTile("host", "device.host", width: 3, height: 1) {
        	state "val", label:'Host of the controller\r\n${currentValue}', defaultState: true
		} 
    
    	valueTile("dni", "device.dni", width: 3, height: 1) {
        	state "val", label:'Identifier\r\n${currentValue}', defaultState: true
		}        
        
        //st.unknown.zwave.static-controller
        //st.Office.office5
		main("status")
		details(["status", "host", "dni"])
	}
}

def installed() 
{
	log.debug "Installed with settings: ${settings}"
    
    updateDataValue("host", "192.168.144.38:5000")

	configure();
}

def updated() 
{
	log.debug "Updated with settings: ${settings}"

	configure();
}

def configure() 
{
	unschedule();

    runEvery1Minute("pingController");
	pingController();
    
    sendEvent(name: "host", value: getDataValue("host"))
    sendEvent(name: "dni", value: "${device.deviceNetworkId}")
}

def updateHost(newHost) 
{
	def oldHost = getDataValue("host")
	if (newHost && oldHost != newHost) 
	{
		log.debug "Host updated: ${newHost} (was: ${oldHost})"

		updateDataValue("host", newHost);
        sendEvent(name: "host", value: newHost);
        pingController();
	}
}

def refresh() {
	log.debug "Executing 'refresh'"
    
    pingController();
}

def pingController() 
{
	// First update the current status if the controller did not replied since more than 5 min
	def currentState = device.currentState("multiplexerStatus");
    if (currentState)
    {
    	log.debug "Ping controller (last status reported: ${currentState.value} @ ${currentState.date})"
    	if (currentState.value == "online")
        use( TimeCategory ) {
            if(currentState.date + 5.minutes < new Date())
            {
            	log.debug "Controller didn't replied to ping since more than 5 minutes, report it as offline."
            	sendEvent(name: "multiplexerStatus", value: "offline")
            }
        }
    }
    else
    {
    	log.debug "Ping controller (Status was not reported yet)"
    }

	// Send ping request
	def host = getDataValue("host");
    def id = device.deviceNetworkId;
    def command = new physicalgraph.device.HubAction(
        method: "GET",
        path: "/api/ping",
        headers: [
        	Host: host,
            "Smartthings-Device": id]
    );
    
    def result = sendHubCommand(command)
    
    log.debug "Sent ${command} => ${result}"
}

def parse(String description) {
	log.debug "Received message '${description}'."
    
    def message = description
    	.split(', ')
        .collectEntries{ it -> it.split(':', 2).with { [(it[0]): (it.drop(1).find { true })] } };
    
    if (!message.headers)
    {
    	log.debug "Unknown message, ignore it.";
        return;
    }
        
    def headers = new String(message.headers.decodeBase64())
    	.split('\r\n')
        .drop(1) // Skip the request line ("NOTIFY /api/devices HTTP/1.1")
        .collectEntries { header -> header.split(': ', 2).with { [(it[0]): (it.drop(1).find { true })] } };
    def deviceId = headers["Smartthings-Device"];
    
    if(!deviceId)
    {
    	log.debug "Target device not set, ignore the message.";
        return;
    }
    
    // As we received a message whith the device identifier, we can assume that the controller is online
    sendEvent(name: "multiplexerStatus", value: "online")
    
    if (deviceId == device.deviceNetworkId)
    {
    	log.debug "Message is for this controller, handle it locally."
    	parseLocal(description);
        return;
    }
    
    def target = parent.getChildDevice(deviceId);
    if (target)
    {
    	log.debug "Forwarding the message to device '${target}' (id: ${deviceId})."
    	target.parse(description);
        return;
    }

	log.error "Device '${deviceId}' not found, cannot forward the message."
}

def parseLocal(String description) {
	
}