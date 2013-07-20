'''***************************************************************************
'* Descripción: Ejemplo del uso de la clase WSConecFM de Facturacion Moderna para el
'* Timbrado y cancelacion de un comprobante generando un
'* archivo XML de un CFDI 3.2 y enviandolo a certificar.
'*
'* Nota: Esté ejemplo pretende ilustrar de manera general el proceso de sellado y
'* timbrado de un XML que cumpla con los requerimientos del SAT.
'*
'* Facturación Moderna :  http://www.facturacionmoderna.com
'* @author Benito Arango <benito.arango@facturacionmoderna.com>
'* @version 1.0
'*
'*****************************************************************************

Imports System.Collections.Generic
Imports System.ComponentModel
Imports System.Data
Imports System.Drawing
Imports System.Linq
Imports System.Text
Imports System.Threading.Tasks
Imports System.Windows.Forms
Imports Comprobante
Imports WSConecFM

Public Class Form1

  Private Sub Button1_Click(sender As Object, e As EventArgs) Handles Button1.Click
    ' Especificación de rutas especificas
    Dim keyfile As String = "C:\FacturacionModernaVB\utilerias\certificados\20001000000200000192.key"
    Dim certfile As String = "C:\FacturacionModernaVB\utilerias\certificados\20001000000200000192.cer"
    Dim password As String = "12345678a"
    Dim xsltfile As String = "C:\FacturacionModernaVB\utilerias\xslt32\cadenaoriginal_3_2.xslt"
    Dim xmlfile As String = TextBox1.Text
    Dim originalPath As String = "C:\FacturacionModernaVB\resultados\cadenaOriginal.txt"
    Dim path As String = "C:\FacturacionModernaVB\resultados"
    Dim numCert As String = "20001000000200000192"

    ' Generar la cadena original
    ' Crear instancias de la clase cadena
    Dim cadena As New Cadena()
    'Llamar la funcion generar cadena, (Regresa un arreglo, status y mensaje), enviando como parametros:
    ' 1.- Ruta del archivo xslt, el cual contiene el esquema de la cadena original
    ' 2.- Ruta del archivo xml, del cual se tomaran los datos para generar la cadena original
    ' 3.- Ruta donde se guardará el archivo de la cadena original
    Dim r_cadena As String() = cadena.GeneraCadena(xsltfile, xmlfile, originalPath)
    If r_cadena(0) = "1" Then
      MessageBox.Show(r_cadena(1))
      Me.Close()
    End If

    ' Generar el sello del comprobante
    ' Crear instancia de la clase Sello
    Dim sello As New Sello()
    'Llamar la funcion generar sello, (Regresa un arreglo, status y mensaje), enviando como parametros:
    ' 1.- Ruta de archivo de llave privada (.key)
    ' 2.- Contraseña del archivo de llave privada
    ' 3.- Ruta del archivo que contiene la cadena original
    Dim r_sello As String() = sello.GeneraSello(keyfile, password, originalPath)
    If r_sello(0) = "1" Then
      MessageBox.Show(r_sello(1))
      Me.Close()
    End If
    Dim str_sello As String = r_sello(1)

    ' Obtener el contenido del certificado
    ' Extrae el contenido del certificado (.cer)
    ' Enviar como parametro la ruta del archivo del certificado
    Dim r_certificado As String() = sello.obtenCertificado(certfile)
    If r_certificado(0) = "1" Then
      MessageBox.Show(r_certificado(1))
      Me.Close()
    End If
    Dim str_certificado As String = r_certificado(1)

    ' Agregar el sello generado al comprobante
    ' Los parametros son:
    ' 1.- Ruta del archivo XML
    ' 2.- Cadena del sello digital
    ' 3.- Cadena del certificado
    ' 4.- Número de certificado (.cer)
    Dim r_agregasello As String() = sello.agregaSello(xmlfile, str_sello, str_certificado, numCert)
    If r_agregasello(0) = "1" Then
      MessageBox.Show(r_agregasello(1))
      Me.Close()
    End If

    ' Crear instancia a la clase de timbrado
    Dim conex As New Timbrado()

    ' Crear instancia a requestTimbrarCFDI, Regresa los parametros necesarios para poder realziar la conexion SOAP (Ya cuenta con los parametros necesarios para la conexion)
    ' Los posibles valores son:
    ' string text2CFDI: Archivo XML codificado en base 64 para ser timbrado
    ' string UserID: Nombre de usuario para conexion con SOAP
    ' string UserPass: Contraseña de usuario para conexion con SOAP
    ' string emisorRFC: RFC del emisor
    ' Boolean generarCBB: Retorna el Codigo de Barras Bidimension, Si esta es TRUE, generarPDF y generarTXT se convierten en FALSE
    ' Boolean generarTXT: Retorna el comprobante en TXT
    ' Boolean generarPDF: Retorna el comprobante en PDF
    ' string urltimbrado: URL de acceso al servisio SOAP
    Dim reqt As New requestTimbrarCFDI()
    ' Para cambiar un valor hacer lo del siguiente ejemplo:
    ' reqt.generarPDF = true;

    ' Ejecutar Timbrado del comprobante
    Dim r_timbrar As String() = conex.Timbrar(xmlfile, reqt, path)
    MessageBox.Show(r_timbrar(1))

    ' Fin de Timbrado
    Me.Close()
  End Sub

  Private Sub Button2_Click(sender As Object, e As EventArgs) Handles Button2.Click
    Dim layoutFile As String = TextBox2.Text
    Dim path As String = "C:\FacturacionModernaVB\resultados"

    ' Crear instancia a la clase de timbrado
    Dim conex As New Timbrado()

    ' Crear instancia, para los para metros enviados a requestTimbradoCFDI
    Dim reqt As New requestTimbrarCFDI()

    ' Ejecutar Timbrado del comprobante
    Dim r_timbrar As String() = conex.Timbrar(layoutFile, reqt, path)
    MessageBox.Show(r_timbrar(1))

    ' Fin de Timbrado
    Me.Close()
  End Sub

  Private Sub Button3_Click(sender As Object, e As EventArgs) Handles Button3.Click
    ' Cancelar un comprobante por medio de su UUID
    ' Crear la instancia de la clase Cancelado
    Dim conex As New Cancelado()
    ' Crear instancia a requestCancelarCFDI, Regresa los parametros necesarios para poder realziar la conexion SOAP (Ya cuenta con los parametros necesarios para la conexion)
    ' Los posibles valores son:
    ' string UserID: Nombre de usuario para conexion con SOAP
    ' string UserPass: Contraseña de usuario para conexion con SOAP
    ' string emisorRFC: RFC del emisor
    ' Boolean uuid: UUID que se va a cancelar
    ' string urlcancelado: URL de acceso al servisio SOAP
    Dim reqc As New requestCancelarCFDI()
    reqc.uuid = TextBox3.Text

    ' Ejecutar Cancelado del comprobante
    Dim r_cancelar As String() = conex.Cancelar(reqc)
    MessageBox.Show(r_cancelar(1))

    Me.Close()
  End Sub
End Class
