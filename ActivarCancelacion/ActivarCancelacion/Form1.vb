Imports WSConecFM

Public Class Form1

    Private Sub btnActivar_Click(sender As Object, e As EventArgs) Handles btnActivar.Click
        Dim fileCer As String = txtCer.Text
        Dim fileKey As String = txtKey.Text
        Dim keyPass As String = txtPass.Text

        Cursor.Current = Cursors.WaitCursor
        Dim r_wsconect As New WSConecFM.Resultados()

        '  CREAR LA CONFIGURACION DE CONEXION CON EL SERVICIO SOAP
        '             * *    Los parametros configurables son:
        '             * *    1.- string UserID; Nombre de usuario que se utiliza para la conexion con SOAP
        '             * *    2.- string UserPass; Contraseña del usuario para conectarse a SOAP
        '             * *    3.- string emisorRFC; RFC del contribuyente
        '             * *    4.- string archivoKey; archivo llave
        '             * *    5.- string archivoCer; archivo de certificado
        '             * *    6.- string clave; contraseña del archivo llave del certificado
        '             * *    7.- string urlActivarCancelacion; URL de la conexion con SOAP
        '             * La configuracion inicial es para el ambiente de pruebas
        '            

        Dim activarC As New WSConecFM.activarCancelacion()
        '
        '             * Si desea cambiar alguna configuracion realizarla solo realizar lo siguiente
        '             * activarC.UserID = "Miusuario";  Por poner un ejemplo
        '            

        activarC.archivoCer = fileCer
        activarC.archivoKey = fileKey
        activarC.clave = keyPass

        Dim activation As New ActivarCancelado()
        r_wsconect = activation.Activacion(activarC)
        Cursor.Current = Cursors.[Default]
        MessageBox.Show(r_wsconect.message)
        Close()
    End Sub
End Class
