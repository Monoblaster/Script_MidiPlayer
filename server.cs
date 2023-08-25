function CustomMidiPlayer_Frequency(%n)
{
	return 440 * mpow(2, ((%n - 69) / 12));
}

$CustomMidiPlayer::Drum2Instrument = ""
@ ""
SPC "HC1"
SPC "RScr1"
SPC "RScr2"
SPC "SS"
SPC "SS"
SPC ""
SPC "BD"
SPC "BD"
SPC "SS"
SPC "HC2"
SPC "808SD"
SPC "TF"
SPC "HHC"
SPC "TF"
SPC "HHC"
SPC "TL"
SPC "HHO"
SPC "TM"
SPC "TH"
SPC "CC"
SPC "TH"
SPC "RC"
SPC "CC"
SPC "RCB"
SPC ""
SPC "CC"
SPC "CB"
SPC "CC"
SPC ""
SPC "RC"
SPC "CH"
SPC "CL"
SPC "CM"
SPC "CM"
SPC "CM"
SPC "CH"
SPC "CL"
SPC "CH"
SPC "CL"
SPC ""
SPC ""
SPC ""
SPC ""
SPC ""
SPC ""
SPC ""
SPC ""
SPC ""
SPC ""
SPC ""
SPC ""
SPC ""
SPC ""
SPC ""
SPC ""
SPC ""
SPC ""
SPC ""
SPC "SDJ"
SPC "SDO"
SPC "SDB";

$CustomMidiPlayer::Patch2Instrument = ""
@ "Piano"
SPC "Piano"
SPC "Piano"
SPC "Piano"
SPC "Synth"
SPC "Synth"
SPC "Harpsichord"
SPC "Sawtooth"

SPC "Harp"
SPC "Harp"
SPC "Harp"
SPC "Harp"
SPC "Marimba"
SPC "Marimba"
SPC "Harp"
SPC "Sitar"

SPC "Strings"
SPC "Strings"
SPC "Strings"
SPC "Strings"
SPC "Strings"
SPC "Melodica"
SPC "Harmonica"
SPC "Melodica"

SPC "Guitar"
SPC "Guitar"
SPC "Guitar"
SPC "Guitar"
SPC "Guitar"
SPC "ElectricGuitar"
SPC "ElectricGuitar"
SPC "Guitar"

SPC "Guitar"
SPC "Guitar"
SPC "Guitar"
SPC "Guitar"
SPC "Guitar"
SPC "Guitar"
SPC "Synth"
SPC "Synth"

SPC "Violin"
SPC "Violin"
SPC "Strings"
SPC "Strings"
SPC "Strings"
SPC "Strings"
SPC "Strings"
SPC "Bass"

SPC "Strings"
SPC "Strings"
SPC "Synth"
SPC "Synth"
SPC "Microphone"
SPC "Microphone"
SPC "Microphone"
SPC "Synth"

SPC "Brass"
SPC "Brass"
SPC "Brass"
SPC "Brass"
SPC "FrenchHorn"
SPC "Brass"
SPC "Sawtooth"
SPC "Sawtooth"

SPC "Saxophone"
SPC "Saxophone"
SPC "Saxophone"
SPC "Saxophone"
SPC "Saxophone"
SPC "Saxophone"
SPC "Saxophone"
SPC "Saxophone"

SPC "Flute"
SPC "Flute"
SPC "Flute"
SPC "Flute"
SPC "Flute"
SPC "Flute"
SPC "Flute"
SPC "Flute";

function CustomMidiPlayer_GenerateMidi2Note()
{
	$CustomMidiPlayer::Midi2Note = "";
	%notes = "c cs d ds e f fs g gs a as b";
	%notesCount = getWordCount(%notes);
	%c3 = CustomMidiPlayer_Frequency(48);
	%c6 = CustomMidiPlayer_Frequency(84);
	%currNote = 0;
	%currOcatve = -1;
	%s = "";
	for(%i = 0; %i <= 127; %i++)
	{
		%frequency = CustomMidiPlayer_Frequency(%i);
		%note = "";

		if(%frequency < %c3)
		{
			%percent = %frequency / %c3;
			if(%percent >= 0.2)
			{
				%note = "c3:" @ mFloatLength(%percent, 3);
			}
		}
		else if(%frequency > %c6)
		{
			%percent = %frequency / %c6;
			if(%percent <= 2)
			{
				%note = "c6:" @ mFloatLength(%percent, 3);
			}
		}
		else
		{
			%note = getWord(%notes,%currNote) @ %currOcatve;
		}

		%s = %s SPC %note;
		%currNote += 1;
		if(%currNote >= %notesCount)
		{
			%currOcatve += 1;
			%currNote = 0;
		}
	}
	$CustomMidiPlayer::Midi2Note = getSubStr(%s,1,999999);
}
CustomMidiPlayer_GenerateMidi2Note();

function serverCmdSendMidi(%client,%a,%b,%c,%d,%e,%f,%g,%h,%i,%j,%k,%l,%m,%n,%o,%p,%q,%r)
{
	%client.CustomMidiPlayer_NoAck = false;
	%string = %a @ %b @ %c @ %d @ %e @ %f @ %g @ %h @ %i 
	@ %j @ %k @ %l @ %m @ %n @ %o @ %p @ %q @ %r;
	switch$(%a)
	{
	case "start":
		%client.CustomMidiPlayer_FileName = %b;
		%client.CustomMidiPlayer_TicksPerBeat = %c;
		%client.CustomMidiPlayer_MicrosecondsPerBeat = 500000;
		%client.CustomMidiPlayer_ElapsedTicks = 0;
		%client.CustomMidiPlayer_FileEvents = "";
	case "ready":
		%client.CustomMidiPlayer_FileEvents = trim(%client.CustomMidiPlayer_FileEvents);
		CustomMidiPlayer_Play(%client);
	default:
		//substr limits string length
		%event = trim(%name SPC %a SPC %b SPC %c SPC %d SPC %e SPC %f SPC %g SPC %h SPC %i SPC %j);
		if(%event !$= "")
		{
			getsubstr(%client.CustomMidiPlayer_FileEvents = %client.CustomMidiPlayer_FileEvents 
			@ %string,0,999999);
		}
	}
}

function CustomMidiPlayer_Play(%client)
{
	cancel(%client.CustomMidiPlayer_Schedule);
	CustomMidiPlayer_PlayLoop(%client);
}

function CustomMidiPlayer_PlayLoop(%client,%lastTime)
{
	%delta = getRealTime() - %lastTime;
	if(%lastTime $= "")
	{
		%delta = 0;
	}
 	%currTick = %client.CustomMidiPlayer_ElapsedTicks += ((%delta * 1000) / %client.CustomMidiPlayer_MicrosecondsPerBeat) * %client.CustomMidiPlayer_TicksPerBeat;

	%player = %client.player;
	if(!isObject(%player))
	{
		return;
	}

	%event = getField(%client.CustomMidiPlayer_FileEvents,0);

	if(%currTick <= getWord(%event,1))
	{
		%client.CustomMidiPlayer_Schedule = schedule(33,%client,"CustomMidiPlayer_PlayLoop",%client,getRealTime());
		return;
	}

	while(%currTick > getWord(%event,1))
	{
		%name = getWord(%event,0);
		switch$(%name)
		{
		case "patch_change":
			%client.CustomMidiPlayer_ChannelPatch[getword(%event,2)] = getword(%event,3);
		case "set_tempo":
			%client.CustomMidiPlayer_MicrosecondsPerBeat = getWord(%event,2);
		case "note":
			%sound = "";
			%instrument = getWord($CustomMidiPlayer::Patch2Instrument,%client.CustomMidiPlayer_ChannelPatch[getword(%event,3)]);
			%note = getWord($CustomMidiPlayer::Midi2Note,getWord(%event,4));
			if(getword(%event,3) == 9)
			{	
				%instrument = "Drums";
				%note = getWord($CustomMidiPlayer::Drum2Instrument,getWord(%event,4) - 27);
			}

			if(%note !$= "")
			{
				// Pitch-changing
				%colonPos = strPos(%note, ":");
				%pitch = 1;

				if (%colonPos != -1) {
					%pitch = getSubStr(%note, %colonPos + 1, 999999);
					%note = getSubStr(%note, 0, %colonPos);
				}
				

				%sound = InstrumentsServer.getNoteSound(%instrument, %note);
				%pitch--;
				%pitch += InstrumentsServer.getNotePitch(%note);

				%zScale = mRound(getWord(%player.getScale(), 2) / 2);
				%pos = vectorAdd(%player.getPosition(), "0 0" SPC %zScale);
				InstrumentsServer.playPitchedSound(%player, %sound, %pitch, %pos);
			}
		}

		if(!%client.CustomMidiPlayer_NoAck && getFieldCount(%client.CustomMidiPlayer_FileEvents) < 50)
		{
			%client.CustomMidiPlayer_NoAck = true;
			commandToClient(%client, 'CustomMidiPlayerNext');
		}

		%client.CustomMidiPlayer_FileEvents = removeField(%client.CustomMidiPlayer_FileEvents,0);
		%event = getField(%client.CustomMidiPlayer_FileEvents,0);
		if(%event $= "")
		{
			
			return;
		}
	}
	%client.CustomMidiPlayer_Schedule = schedule(33,%client,"CustomMidiPlayer_PlayLoop",%client,getRealTime());
}