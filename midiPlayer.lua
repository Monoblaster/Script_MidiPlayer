local midi = dofile('./MIDI-6.9/MIDI.lua')

local function send(cmd,...)
	ts.call('commandToServer',cmd,...)
end

local function frequency(n)
	return 440 * math.pow(2, ((n - 69) / 12))
end


local function midi2note(currnum,seconds)
	seconds = seconds or 0.125
	local notes = {'c','cs','d','ds','e','f','fs','g','gs','a','as','b'}
	local lowestoctave = 3
	local lowestnum = 48
	local highestoctave = 6
	local highestnum = lowestnum + (highestoctave - lowestoctave) * 12
	local lengthmodifier = math.floor((seconds - 0.125) * 2)

	local curroctave = math.floor(currnum / 12 - 1)
	local shiftnum = (lengthmodifier * 12) + (curroctave + 1) * 12
	local percent = frequency(currnum) / frequency(shiftnum)
	while (percent <= 0.2 or percent >= 2  or shiftnum > highestnum) and lengthmodifier ~= 0 do
		if lengthmodifier < 0 then
			lengthmodifier = math.floor(lengthmodifier + 1)
		else
			lengthmodifier = math.floor(lengthmodifier - 1)
		end
		shiftnum = (lengthmodifier * 12) + (curroctave + 1) * 12
		percent = frequency(currnum) / frequency(shiftnum)
	end

	if shiftnum < lowestnum then
		shiftnum = lowestnum
	end

	if shiftnum > highestnum then
		shiftnum = highestnum
	end

	percent = frequency(currnum) / frequency(shiftnum)
	if percent >= 2 then
		return ""
	end
	if shiftnum ~= (curroctave + 1) * 12 or ((shiftnum == (curroctave + 1) * 12) and currnum > highestnum) then
		local shiftoctave = math.floor(shiftnum / 12 - 1)
		return notes[1] .. shiftoctave .. ':' .. string.format('%.3f',percent)
	else
		return notes[currnum % 12 + 1] .. curroctave
	end
end

PlayingStreamer = PlayingStreamer or nil
PlayingEventList = PlayingEventList or nil
PlayingName = PlayingName or ""
PlayingTicksPerBeat = PlayingTicksPerBeat or 0
PlayingIndex = PlayingIndex or 0

local function goto(ms)
	PlayingIndex = 0
	local elapsedms = 0
	local microsendsperbeat = 500000
	local lasttick = 0
	while elapsedms < ms and PlayingIndex < #PlayingEventList do
		local event = PlayingEventList[PlayingIndex + 1]
		local currtick = event[2]
		if event[1] == 'set_tempo' then
			microsendsperbeat = event[3]
		end
		elapsedms = elapsedms + math.floor(((currtick - lasttick) / PlayingTicksPerBeat) * microsendsperbeat) / 1000
		lasttick = currtick
		PlayingIndex = PlayingIndex + 1
	end
	return microsendsperbeat
end

local function play(filepath,startms,method)
	startms = tonumber(startms)
	local file = io.open(filepath,'rb')
	if file then
		local score = midi.midi2score(file:read('*a'))
		
		file:close()
		local tracks = {}
		for i = 2, #score, 1 do
			for _, value in ipairs(score[i]) do
				if method.events[value[1]] then
					table.insert(tracks, value)
				end
			end
		end

		table.sort(tracks, function (a, b)
			return a[2] < b[2]
		end)
		
		PlayingName = string.match(filepath,'.+/(.-)%.mid')
		PlayingTicksPerBeat = score[1]
		PlayingEventList = tracks
		PlayingIndex = 0
		PlayingStreamer = ''
		method:start(startms)
		PlayingStreamer = method

		
	else
		print('failed to open')
	end
end

local function tick2ms(nspertick,ticksperbeat,ticks)
	return math.floor(((ticks / ticksperbeat) * nspertick) / 1000)
end

local function ms2tick(nspertick,ticksperbeat,ms)
	return math.floor((ms * 1000 / nspertick) * ticksperbeat)
end

CustomPlayer = {
	events = {note = true,patch_change = true,set_tempo = true},

	start = function (o,startms)
		local tag = ts.call('addTaggedString','SendMidi')
		send(tag,'start',PlayingName,PlayingTicksPerBeat)
		if startms then
			send(tag,'set_tempo',0,goto(startms))
		end
		o:next(255,10)
		send(tag,'ready')
	end,
	
	next = function (o)
		local tag = ts.call('addTaggedString','SendMidi')
		local chunksize = 255;
		local numchunks = 10

		-- convert our events into a string to chunk and send
		local stringsize = chunksize * numchunks
		local s = ''
		local iterator = ipairs(PlayingEventList)
		for _, event in iterator, PlayingEventList, PlayingIndex do
			local check = s .. '\t' .. table.concat(event,' ')
			if #check < stringsize then
				s = check
				PlayingIndex = PlayingIndex + 1
			else
				break
			end
		end
		s = string.sub(s,2)

		if #s == 0 then
			send(tag,"end")
			return
		end
	
		local tracksstrings = {}
		for i = 1, #s, chunksize do
			table.insert(tracksstrings,string.sub(s,i,i + chunksize - 1))
		end
	
		
		local chuckspercmd = 4
		for i = 1, #tracksstrings, chuckspercmd do
			send(tag,unpack(tracksstrings,i,i + chuckspercmd - 1))
		end
	end
}

InstrumentsPlayer = {
	events = {note = true,set_tempo = true},
	tickdifference = 0,
	currtick = 0,
	microsendsperbeat = 0,
	lastdelay = 0,
	splitchord = false,
	currdelay = 0,
	currchord = {},

	start = function (o,startms)
		o.microsendsperbeat = 500000
		if startms then
			o.microsendsperbeat = goto(startms)
		end
		o.tickdifference = 0
		o.lastdelay = 0
		o.currdelay = 06
		o.currtick = 0
		o.splitchord = false
		o.currchord = {}
		send(ts.call('addTaggedString','StopPlayingInstrument'))
	end,

	next = function (o)
		local setadelay = false
		--done to prevent delays from not being set on nexts
		o.lastdelay = 0
		--convert events into insrtrument events
		local appendto = ''
		local maxSongPhrases = ts.get('Instruments::Client::ServerPref::MaxSongPhrases') - 1
		for currphrase = 0, maxSongPhrases do
			local phrase = appendto
			while PlayingIndex < #PlayingEventList do
				-- fill out a phrase to send
				if #o.currchord == 0 then
					-- fill out a list of notes on the same beat
					if not PlayingEventList then
						return
					end
					local iterator = ipairs(PlayingEventList)
		
					local newindex = PlayingIndex
					for index, event in iterator,PlayingEventList,PlayingIndex do
						-- notes
						if event[1] == 'note' then
							if event[4] == 9 then
								goto continue
							end

							if midi2note(event[5]) == "" then
								goto continue
							end
							
							local starttick = event[2]
							if starttick - o.currtick < 0 or tick2ms(o.microsendsperbeat,PlayingTicksPerBeat,starttick -  o.currtick) <= 33 then
								
								o.currchord[#o.currchord + 1] = event
								goto continue
							end
		
							o.tickdifference = starttick - o.currtick
							o.currtick = starttick
							break
						end
						
						--tempo change
						if event[1] == 'set_tempo' then
							o.microsendsperbeat = event[3]
						end
		
						::continue::
						newindex = index
					end
					PlayingIndex = newindex
				end
				
				-- remove duplicate notes from the chord
				local count = 1
				while #o.currchord > 0 and count <= #o.currchord do
					local curr = o.currchord[count]
					local next = count + 1
					while next <= #o.currchord do
						local value = o.currchord[next]
						if curr[5] == value[5] then
							if curr[3] < value[3] then
								table.remove(o.currchord,count)
								count = count - 1
								break
							end

							table.remove(o.currchord,next)
							next = next - 1
						end
						next = next + 1
					end
					count = count + 1
				end
				
				-- add onto the phrase using the new chord
				local notes = ''
				count = 1
				while #o.currchord > 0 and count <= 4 do
					local event = table.remove(o.currchord,1)
					local note = midi2note(event[5],math.floor(tick2ms(o.microsendsperbeat,PlayingTicksPerBeat,event[3]) / 1000))

					if note then
						notes = notes .. note .. '+'
						count = count + 1
					end
				end
				notes = string.sub(notes,1,#notes - 1)

				-- crush notes if we're too ful
				if #o.currchord >= 4  then
					local nextevent = PlayingEventList[PlayingIndex]
					if nextevent and nextevent[1] == 'note' and nextevent[4] ~= 9 and nextevent[2] < o.currtick then
						o.currchord = {}
					end
				end
				
				local currdelay = tick2ms(o.microsendsperbeat,PlayingTicksPerBeat,o.tickdifference)
				if #o.currchord > 0 then
					--increase the tick difference for the next part of the chord
					local tickadd = ms2tick(o.microsendsperbeat,PlayingTicksPerBeat,34)
					currdelay = 34
					o.tickdifference = o.tickdifference - tickadd
					o.currtick = o.currtick + tickadd
				end

				local delay = ''
				if currdelay ~= o.lastdelay or not setadelay then
					-- we need to set a new delay
					setadelay = true
					delay = 'd:' .. currdelay .. ','
					o.lastdelay = currdelay
				end
				appendto = delay .. notes

				local newphrase = phrase .. ',' .. appendto
				if #newphrase > 255 then
					break
				end
				appendto = ''
				phrase = newphrase
			end
			send(ts.call('addTaggedString','SetSongPhrase'),currphrase,string.sub(phrase,1),true)
		end

		local s = ""
		for i = 0, maxSongPhrases do
			s = s .. "," .. i
		end
		s = string.sub(s,2)
		send(ts.call('addTaggedString','PlaySong'),s)
	end
}

function MidiPlayer_Next()
	if PlayingStreamer then
		PlayingStreamer:next()
	end
end

function MidiPlayer_PlayInstruments(filepath,startms)
	play(filepath,startms,InstrumentsPlayer)
end

function MidiPlayer_PlayCustom(filepath,startms)
	play(filepath,startms,CustomPlayer)
end