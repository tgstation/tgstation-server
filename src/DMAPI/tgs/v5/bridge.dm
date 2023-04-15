/datum/tgs_api/v5/proc/Bridge(command, list/data)
	if(!data)
		data = list()

	var/single_bridge_request = CreateBridgeRequest(command, data)
	if(length(single_bridge_request) <= DMAPI5_BRIDGE_REQUEST_LIMIT)
		return PerformBridgeRequest(single_bridge_request)

	// chunking required
	var/payload_id = ++chunked_requests

	var/raw_data = CreateBridgeData(command, data, FALSE)
	var/data_length = length(raw_data)

	var/chunk_count
	var/list/chunk_requests
	for(chunk_count = 2; !chunk_requests; ++chunk_count);
		var/max_chunk_size = -round(-(data_length / chunk_count))
		if(max_chunk_size > DMAPI5_BRIDGE_REQUEST_LIMIT)
			continue

		chunk_requests = list()
		for(var/i in 1 to chunk_count)
			var/startIndex = 1 + ((i - 1) * max_chunk_size)
			var/endIndex = min(1 + (i * max_chunk_size), data_length + 1)
			var/chunk_payload = copytext(raw_data, startIndex, endIndex)
			var/list/chunk = list("payloadId" = payload_id, "sequenceId" = (i - 1), "totalChunks" = chunk_count, payload = chunk_payload)

			var/chunk_request = CreateBridgeRequest(DMAPI5_BRIDGE_COMMAND_CHUNK, list("chunk" = chunk))
			if(length(chunk_request) > DMAPI5_BRIDGE_REQUEST_LIMIT)
				// Screwed by url encoding, no way to preempt it though
				chunk_requests = null
				break

			chunk_requests += chunk_request

	var/list/response
	for(var/bridge_request in chunk_requests)
		response = PerformBridgeRequest(bridge_request)
		if(!response)
			// Abort
			return

	var/list/missing_sequence_ids = response[DMAPI5_BRIDGE_RESPONSE_MISSING_CHUNKS]
	if(length(missing_sequence_ids))
		do
			TGS_WARNING_LOG("Server is missing some chunks of payload [payload_id]! Sending missing chunks...")
			if(!istype(missing_sequence_ids))
				TGS_ERROR_LOG("Did not receive a list() for [DMAPI5_BRIDGE_RESPONSE_MISSING_CHUNKS]!")
				return

			for(var/missing_sequence_id in missing_sequence_ids)
				if(!isnum(missing_sequence_id))
					TGS_ERROR_LOG("Did not receive a num in [DMAPI5_BRIDGE_RESPONSE_MISSING_CHUNKS]!")
					return

				var/missing_chunk_request = chunk_requests[missing_sequence_id + 1]
				response = PerformBridgeRequest(missing_chunk_request)
				if(!response)
					// Abort
					return

			missing_sequence_ids = response[DMAPI5_BRIDGE_RESPONSE_MISSING_CHUNKS]
		while(length(missing_sequence_ids))

	return response

/datum/tgs_api/v5/proc/CreateBridgeRequest(command, list/data)
	var/json = CreateBridgeData(command, data, TRUE)
	var/encoded_json = url_encode(json)

	var/url = "http://127.0.0.1:[server_port]/Bridge?[DMAPI5_BRIDGE_DATA]=[encoded_json]"
	return url

/datum/tgs_api/v5/proc/CreateBridgeData(command, list/data, needs_auth)
	data[DMAPI5_BRIDGE_PARAMETER_COMMAND_TYPE] = command
	if(needs_auth)
		data[DMAPI5_PARAMETER_ACCESS_IDENTIFIER] = access_identifier

	var/json = json_encode(data)
	return json

/datum/tgs_api/v5/proc/PerformBridgeRequest(bridge_request)
	// This is an infinite sleep until we get a response
	var/export_response = world.Export(bridge_request)
	if(!export_response)
		TGS_ERROR_LOG("Failed bridge request: [bridge_request]")
		return

	var/response_json = file2text(export_response["CONTENT"])
	if(!response_json)
		TGS_ERROR_LOG("Failed bridge request, missing content!")
		return

	var/list/bridge_response = json_decode(response_json)
	if(!bridge_response)
		TGS_ERROR_LOG("Failed bridge request, bad json: [response_json]")
		return

	var/error = bridge_response[DMAPI5_RESPONSE_ERROR_MESSAGE]
	if(error)
		TGS_ERROR_LOG("Failed bridge request, bad request: [error]")
		return

	return bridge_response
