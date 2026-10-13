# Offline fictional syllables, never spoken skill names or a real person's voice.
$ErrorActionPreference='Stop'
Add-Type -AssemblyName System.Speech
$output=Join-Path (Split-Path -Parent $PSScriptRoot) 'Temp\SkillChants'
New-Item -ItemType Directory -Force -Path $output | Out-Null
$synth=New-Object System.Speech.Synthesis.SpeechSynthesizer
try {
    $synth.SelectVoice('Microsoft Zira Desktop')
    $format=New-Object System.Speech.AudioFormat.SpeechAudioFormatInfo(22050,[System.Speech.AudioFormat.AudioBitsPerSample]::Sixteen,[System.Speech.AudioFormat.AudioChannel]::Mono)
    $syllables=@(
        @('Vah! Krah!','Doh rah! Thoom!','Vah roh, karahn!','','Koh vah!','Rah! Thakar!','Ah roh, vah lah!','Drohm, kah rah!','','Kah roh, vah thoom!'),
        @('Iss ah, vey rah!','Ohr dah, kha rohm!','Zeh rah, thah vi ah!','','Zah! Keh rai!','Fey ah, soh rah!','Iss ah, zah rehn!','Voh rah, neh kha thoom!','','Ah zeh, kha rah, voh thoom!'),
        @('Sha! Vey!','Keh rah!','Sey ah, vey shahn!','','Fey rah!','Sih rah, thah vey!','Vey! Sha rah!','Shah veh, sey rah!','','Ah vey, shah rah, seh thoom!'),
        @('Ooh rah, vah!','Thah lah, seh rah!','Aoh rah, vah lehn!','','Seh roh, lah kai!','Zeh! Vah rah!','Kha rah, ah vey!','Ohm, neh rah, thah voh!','','Aoh lah, thah rah, voh rohm!')
    )
    for($hero=0;$hero -lt 4;$hero++) {
        for($skill=0;$skill -lt 10;$skill++) {
            if($skill -eq 3 -or $skill -eq 8){continue}
            $synth.Rate=if($hero -eq 0){1}elseif($hero -eq 2){2}else{0}
            $synth.SetOutputToWaveFile((Join-Path $output "$hero-$skill.wav"),$format)
            $synth.Speak($syllables[$hero][$skill])
            $synth.SetOutputToNull()
        }
    }
    Write-Output 'Generated 32 fictional incantations for offline sound design.'
} finally { $synth.Dispose() }
