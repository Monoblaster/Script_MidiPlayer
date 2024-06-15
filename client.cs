luaexec("./midiplayer.lua");

function clientCmdCustomMidiPlayerNext()
{
	talk("next");
	luacall("MidiPlayer_Next");
}

$MidiPlayer::StartTime = $MidiPlayer::StartTime || getSimTime();
$MidiPlayer::LastStop = 0;
package MidiPlayer
{
	function clientCmdStopPlayingInstrument()
	{	
		parent::clientCmdStopPlayingInstrument();
		if(($MidiPlayer::LastStop + 1000) < getSimTime())
		{
			luacall("MidiPlayer_Next");
		}
		$MidiPlayer::LastStop = getSimTime();
	}
};
activatePackage("MidiPlayer");

function MidiPlayer_Play(%method,%file,%minutes)
{
	$MidiPlayer::StartTime = getSimTime() / 1000 - %minutes * 60;
	%file = findFirstFile("config/client/midi/" @ %file @ "*.mid");	
	echo(%file);
	if(%file !$= "")
	{
		if(%method $= "Instruments")
		{
			luacall("MidiPlayer_PlayInstruments",%file,%minutes * 60000);
			return;
		}

		if(%method $= "Custom")
		{
			luacall("MidiPlayer_PlayCustom",%file,%minutes * 60000);
			return;
		}
	}
	echo("not found!");
}

function MidiPlayer_List(%filepath)
{
	%i = 0;
	%file = findFirstFile("config/client/midi/" @ %filepath @ "*.mid");
	if(%file !$= "")
	{
		echo(%i++ @ ")" SPC filebase(%file));
		while((%file = findNextFile("config/client/midi/" @ %filepath @ "*.mid")) !$= "")
		{
			echo(%i++ @ ")" SPC filebase(%file));
		}
		return"";
	}
	echo("none found!");
}

function MidiPlayer_Time()
{
	echo((getSimTime() / 1000 - $MidiPlayer::StartTime) / 60);
}