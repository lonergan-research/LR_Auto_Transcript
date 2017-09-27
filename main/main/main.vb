﻿Imports CIRS_lib

Public Class main
    Private Sub main_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        'My.Settings.Reset()
        checkDirs("ffmpeg", "ffmpegPath", "http://ffmpeg.zeranoe.com/builds/")
        checkDirs("curl", "curlPath", "https://curl.haxx.se/dlwiz/?type=bin&os=Win64&flav=-&ver=*&cpu=x86_64")
    End Sub

    Private Sub checkDirs(program As String, path As String, url As String) 'Check that dependencies exist
        If My.Settings.Item(path) = "" Then 'Check that the path is not empty
            MsgBox("The directory for " & program & " has not been specified. Please select the 'bin' folder of " & program & " installation/extraction", MsgBoxStyle.ApplicationModal + MsgBoxStyle.SystemModal, "Dependency Not Found")
            While fbdDir.ShowDialog() <> DialogResult.OK And Not IO.File.Exists(fbdDir.SelectedPath & "\" & program & ".exe")
                If MsgBox(program & ".exe not found. Respecify directory?", MsgBoxStyle.YesNo + MsgBoxStyle.SystemModal, "Error") = MsgBoxResult.No Then
                    MsgBox(program & " is required to run this application. Please install " & program & " and then relaunch.", MsgBoxStyle.Critical, "Critical Error")
                    Process.Start(url)
                    End
                End If
            End While
            My.Settings.Item(path) = fbdDir.SelectedPath
            fbdDir.Dispose()
            My.Settings.Save()
        End If
    End Sub

    Private Sub btnImport_Click(sender As Object, e As EventArgs) Handles btnImport.Click
        ofdSelect.Multiselect = True
        If ofdSelect.ShowDialog() = DialogResult.OK Then
            Dim bucket As String = InputBox("Please enter the name of the bucket you wish to upload the file for transcription to. This will default to lr_test_transcript", "Select a bucket directory")

            For Each trackname As String In ofdSelect.FileNames
                If bucket = "" Then
                    bucket = "lr_test_transcript"
                End If
                Dim cmd As New Process
                With cmd
                    .StartInfo = New ProcessStartInfo("cmd", String.Format("/k {0} & {1}", My.Settings.ffmpegPath & "\ffmpeg -y -i """ & trackname & """ -ar " & sampleRate & " -ac 1 """ & IO.Path.GetDirectoryName(trackname) & "\" & IO.Path.GetFileNameWithoutExtension(trackname) & ".flac""", "exit")) 'MP3 to FLAC conversion
                    .Start()
                    .WaitForExit()
                End With

                Dim cirsfile As New CIRS_lib.file
                cirsfile.writeToFile(My.Resources.template.ToString & vbNewLine & "      ""uri"":""gs://" & bucket & "/" & IO.Path.GetFileNameWithoutExtension(trackname) & ".flac""" & vbNewLine & "  }" & vbNewLine & "}", IO.Path.GetDirectoryName(trackname) & "\" & IO.Path.GetFileNameWithoutExtension(trackname) & ".json") 'Write .json file in the same directory
            Next
            MsgBox("MP3 to FLAC conversion and relevant .json file generation complete. Ensure that the FLAC files are uploaded to the specified bucket before requesting transcription.", MsgBoxStyle.ApplicationModal, "Conversion Complete")
            ofdSelect.Dispose()
        End If
        ofdSelect.Multiselect = False
    End Sub

    Private Sub generateFile(ByVal x As String, path As String)
        If IO.File.Exists(path) = False Then
            IO.File.Create(path).Dispose()
        Else
            IO.File.Delete(path) 'Overwrite
        End If
        Try
            Dim objWriter As New IO.StreamWriter(path, True)
            objWriter.WriteLine(x)
            objWriter.Close()
        Catch ex As Exception
            MsgBox("Please close the file first.", MsgBoxStyle.Critical, "Error")
        End Try
    End Sub

    Private Sub btnSelectAuthKey_Click(sender As Object, e As EventArgs) Handles btnSelectSAKey.Click
        If ofdSelect.ShowDialog() = DialogResult.OK Then
            Environment.SetEnvironmentVariable("GOOGLE_APPLICATION_CREDENTIALS", ofdSelect.FileName)
            Dim cmd As New Process
            With cmd
                .StartInfo = New ProcessStartInfo("cmd", String.Format("/k {0} & {1}", "gcloud auth application-default print-access-token > " & AppDomain.CurrentDomain.BaseDirectory & "authkey.txt", "exit")) 'MP3 to FLAC conversion
                .Start()
                .WaitForExit()
            End With
            My.Settings.authToken = My.Computer.FileSystem.ReadAllText(AppDomain.CurrentDomain.BaseDirectory & "authkey.txt")
            IO.File.Delete(AppDomain.CurrentDomain.BaseDirectory & "authkey.txt")
            ofdSelect.Dispose()
        End If
    End Sub

    Private Sub btnExit_Click(sender As Object, e As EventArgs) Handles btnExit.Click
        End
    End Sub
End Class

