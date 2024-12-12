// Alfanet.Bll.RadicacionMasivaBLL
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.OleDb;
using System.IO;
using System.Linq;
using Alfanet.CommonLibrary;
using Alfanet.CommonObject;
using Alfanet.Dal;
using FileHelpers;

public class RadicacionMasivaBLL22
{
    private QueryManager Dal = null;
    private static readonly log4net.ILog log
       = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

    public List<ObjFactura> GetPreview2(string Serie, string naturaleza, string medio, string CodDep, ConfigData config, byte[] file, string fileName, out List<string> result, out string objCacheName, out string Summary, out string SCamposVacios, out string SDuplicadosBD, out string SDuplicadosExcel)
    {
        List<ObjFactura> objList = null;
        List<ObjFactura> objList2 = null;
        result = new List<string>();
        Summary = null;
        DataTable data = null;
        try
        {
            objList = new List<ObjFactura>();
            objList2 = new List<ObjFactura>();
            string serverFileName;
            log.Debug("file" + file + " filename " + fileName);
            bool saved = SaveTempFile(file, fileName, out serverFileName);
            string result2 = string.Empty;
            if (saved)
            {
                data = new DataTable();
                data = ReadDataFromFile(serverFileName, out result2);
                if (data != null)
                {
                    if (data.Columns.Count != 23)
                    {
                        objCacheName = "Error en la estructura del archivo";
                        SCamposVacios = string.Empty;
                        SDuplicadosBD = string.Empty;
                        SDuplicadosExcel = string.Empty;
                        return objList;
                    }
                    string[] partes = Serie.Split('|');
                    naturaleza = naturaleza.Remove(naturaleza.IndexOf(" | "));
                    medio = medio.Remove(medio.IndexOf(" | "));
                    Serie = Serie.Remove(Serie.IndexOf(" | "));
                    string faltantes = string.Empty;
                    string Sdup = string.Empty;
                    string SCampos = string.Empty;
                    string SDupExcel = string.Empty;
                    objList = CreateFacturaList(serverFileName, data, naturaleza, Serie, partes[1].ToString(), medio, config, out faltantes, out SCampos, out Sdup, out SDupExcel);
                    objList2 = CreateFacturaList2(data, naturaleza, Serie, partes[1].ToString(), medio);
                    if (string.IsNullOrWhiteSpace(SDupExcel))
                        result = ValidateFacturaList(objList2);
                    else                    
                        objList.Clear();

                    

                    if (result.Count() > 0)
                    {
                        objCacheName = "No fue posible almacenar los datos en Cache";
                        SCamposVacios = string.Empty;
                        SDuplicadosBD = string.Empty;
                        SDuplicadosExcel = string.Empty;
                        return objList2;
                    }
                    CommonLibrary common = new CommonLibrary();
                    Summary = faltantes;
                    string nameObjectCache = "RadicacionMasivaTable_" + CodDep;
                    string SaveResult;
                    common.SaveObjectInCache(nameObjectCache, objList, out SaveResult);
                    objCacheName = nameObjectCache;
                    SCamposVacios = SCampos;
                    SDuplicadosBD = Sdup;
                    SDuplicadosExcel = SDupExcel;
                    return objList;
                }
                objCacheName = "No hay datos para cargar en el documento";
                SCamposVacios = string.Empty;
                SDuplicadosBD = string.Empty;
                SDuplicadosExcel = string.Empty;
                return objList;
            }
            objCacheName = "El archivo no pudo ser cargado al sistema";
            SCamposVacios = string.Empty;
            SDuplicadosBD = string.Empty;
            SDuplicadosExcel = string.Empty;
            return objList;
        }
        catch (Exception ex)
        {
            log.Error($"Se ha presentado un error {ex.Message} {ex.InnerException}");
            objList = new List<ObjFactura>();
            objCacheName = "Error en la generación de la vista previa.<br/>Posible error en el formato del archivo, o en un tipo de dato.";
            SCamposVacios = string.Empty;
            SDuplicadosBD = string.Empty;
            SDuplicadosExcel = string.Empty;
            return objList;
        }
    }

    private List<ObjFactura> CreateFacturaList(string fileName, DataTable data, string Naturaleza, string serie, string DependenciaNombre, string Medio, ConfigData config, out string Faltantes, out string SCamposVacios, out string SDuplicadosBD, out string SDuplicadosExcel)
    {
        ObjFactura factura = null;
        List<ObjFactura> listDocuments = null;
        Faltantes = "La(s) Procedencia(s) ";
        string Summary = "";
        string CamposVacios = "";
        string DuplicadosBD = "";
        string DuplicadosExcel = "";
        try
        {
            Dal = new QueryManager();
            listDocuments = new List<ObjFactura>();
            double DiasVencimiento = Dal.ObtenerDiasVencimiento(config, Naturaleza);
            foreach (DataRow dr in data.Rows)
            {
                //Se va a crear la lista de registros, sin importar nada


                string facturaValidar = dr["facc_factura"].ToString().Trim();
                if (Dal.ValidarExistenciaUnica(config, facturaValidar, dr["facv_tercero"].ToString().Trim()))
                {
                    factura = new ObjFactura();
                    factura.DependenciaNomDestino = DependenciaNombre;
                    factura.ExpedienteCodigo = dr["facv_tercero"].ToString().Trim() + "_" + dr["facn_recibo"].ToString().Trim();
                    Dal.ValidarExpediente(config, factura.ExpedienteCodigo);
                    factura.GrupoCodigo = "4";
                    factura.Serie = serie;
                    factura.ProcedenciaNUI = dr["facv_tercero"].ToString().Trim();
                    factura.Facn_numero = dr["facn_numero"].ToString();
                    factura.Facn_empresa = dr["facn_empresa"].ToString();
                    factura.Facc_documento = dr["facc_documento"].ToString();
                    factura.Facv_tercero = dr["facv_tercero"].ToString();
                    factura.Facn_ubicacion = dr["facn_ubicacion"].ToString();
                    factura.Facv_total = Convert.ToDouble(dr["facv_total"].ToString());
                    factura.Facc_estado = dr["facc_estado"].ToString();
                    factura.Facc_prefijo = dr["facc_prefijo"].ToString();
                    factura.Facn_factura2 = dr["facn_factura2"].ToString();
                    factura.Facc_factura = dr["facc_factura"].ToString().Trim();
                    factura.Facc_alto_costo = dr["facc_alto_costo"].ToString();
                    factura.Terc_nombre = dr["terc_nombre"].ToString();
                    factura.Facf_confirmacion = ((dr["facf_confirmacion"].ToString() == null) ? DateTime.Now : DateTime.Parse(dr["facf_confirmacion"].ToString()));
                    factura.Facn_recibo = dr["facn_recibo"].ToString();
                    factura.Facv_copago = dr["facv_copago"].ToString();
                    factura.Facv_responsable = dr["facv_responsable"].ToString();
                    factura.Facc_conciliado = dr["facc_conciliado"].ToString();
                    factura.Facv_imputable = dr["facv_imputable"].ToString();
                    factura.Facf_radicado = ((dr["facf_radicado"].ToString() == "") ? DateTime.Now : DateTime.Parse(dr["facf_radicado"].ToString()));
                    factura.FechaRadicacion = DateTime.Now;
                    factura.FechaVencimiento = factura.FechaRadicacion.AddDays(DiasVencimiento);
                    factura.Facf_final = ((dr["facf_final"].ToString() == "") ? DateTime.Now : DateTime.Parse(dr["facf_final"].ToString()));
                    factura.Facc_almacenamiento = dr["facc_almacenamiento"].ToString();
                    factura.Cntc_concepto = dr["cntc_concepto"].ToString();
                    factura.Conc_nombre = dr["conc_nombre"].ToString();
                    factura.NaturalezaCodigo = Naturaleza;
                    factura.WFMovimientoFecha = DateTime.Now;
                    factura.FechaProcedencia = DateTime.Now;
                    factura.MedioCodigo = Medio;
                    factura.FileName = factura.Facc_factura.ToUpper() + "+" + dr["facv_tercero"].ToString().Trim();
                    factura.Detalle = "Registro Oasis: " + dr["facn_numero"].ToString() + " Valor: " + dr["facv_total"].ToString() + " Nit del Prestador: " + dr["facv_tercero"].ToString() + " Responsable: " + dr["facv_responsable"].ToString() + " Unidad Almacenamiento: " + dr["facc_almacenamiento"].ToString() + " Modalidad de Contrato: " + dr["conc_nombre"].ToString();
                    listDocuments.Add(factura);
                }
                else
                {
                    DuplicadosBD = DuplicadosBD + "<br/>la factura " + facturaValidar + " Ya Existe en Base de datos revise los datos del archivo <br />";
                }


            }

            // Se itera sobre los diferentes nits que vienen en el campo facv_tercero del archivo con el fin de validar su procedencia
            foreach (var item in listDocuments.Select(x => x.Facv_tercero).Distinct())
            {
                if (!Dal.ValidarProcedenciaNui(config, item))
                {
                    Summary = (Summary.Contains(item) ? Summary : (Summary + " " + item + ","));
                }
            }

            //Se valida que la lista no traiga facturas repetidas, si lo hace, se eliminan de la lista los elementos repetidos
            //Si el número de diferentes facturas es diferente a la cantidad de documentos que trae el archivo




            Faltantes = Faltantes + Summary + " No Existe(n) en Alfanet ";
            SCamposVacios = CamposVacios;
            SDuplicadosBD = DuplicadosBD;
            SDuplicadosExcel = "";
            return listDocuments;
        }
        catch (Exception ex)
        {
            log.Error($"Se ha presentado un error en CreateFacturaList, {ex.Message} {ex.InnerException}");
            throw;
        }
    }
    private List<ObjFactura> CreateFacturaList2(DataTable data, string Naturaleza, string serie, string DependenciaNombre, string Medio)
    {
        ObjFactura factura = null;
        List<ObjFactura> listDocuments = null;
        try
        {
            listDocuments = new List<ObjFactura>();
            foreach (DataRow dr in data.Rows)
            {
                factura = new ObjFactura();
                factura.DependenciaNomDestino = DependenciaNombre;
                factura.ExpedienteCodigo = dr["facv_tercero"].ToString().Trim() + "_" + dr["facn_recibo"].ToString().Trim();
                factura.GrupoCodigo = "4";
                factura.Serie = serie;
                factura.ProcedenciaNUI = dr["facv_tercero"].ToString().Trim();
                factura.Facn_numero = dr["facn_numero"].ToString();
                factura.Facn_empresa = dr["facn_empresa"].ToString();
                factura.Facc_documento = dr["facc_documento"].ToString();
                factura.Facv_tercero = dr["facv_tercero"].ToString();
                factura.Facn_ubicacion = dr["facn_ubicacion"].ToString();
                factura.Facv_total = Convert.ToDouble(dr["facv_total"].ToString());
                factura.Facc_estado = dr["facc_estado"].ToString();
                factura.Facc_prefijo = dr["facc_prefijo"].ToString();
                factura.Facn_factura2 = dr["facn_factura2"].ToString();
                if (factura.Facc_prefijo != "")
                {
                    factura.Facc_factura = factura.Facc_prefijo + factura.Facn_factura2;
                }
                else
                {
                    factura.Facc_factura = dr["facc_factura"].ToString().Trim();
                }
                factura.Facc_alto_costo = dr["facc_alto_costo"].ToString();
                factura.Terc_nombre = dr["terc_nombre"].ToString();
                factura.Facf_confirmacion = ((dr["facf_confirmacion"].ToString() == null) ? DateTime.Now : DateTime.Parse(dr["facf_confirmacion"].ToString()));
                factura.Facn_recibo = dr["facn_recibo"].ToString();
                factura.Facv_copago = dr["facv_copago"].ToString();
                factura.Facv_responsable = dr["facv_responsable"].ToString();
                factura.Facc_conciliado = dr["facc_conciliado"].ToString();
                factura.Facv_imputable = dr["facv_imputable"].ToString();
                factura.Facf_radicado = ((dr["facf_radicado"].ToString() == "") ? DateTime.Now : DateTime.Parse(dr["facf_radicado"].ToString()));
                factura.FechaRadicacion = DateTime.Now;
                factura.FechaVencimiento = factura.FechaRadicacion.AddDays(2.0);
                factura.Facf_final = ((dr["facf_final"].ToString() == "") ? DateTime.Now : DateTime.Parse(dr["facf_final"].ToString()));
                factura.Facc_almacenamiento = dr["facc_almacenamiento"].ToString();
                factura.Cntc_concepto = dr["cntc_concepto"].ToString();
                factura.Conc_nombre = dr["conc_nombre"].ToString();
                factura.NaturalezaCodigo = Naturaleza;
                factura.WFMovimientoFecha = DateTime.Now;
                factura.FechaProcedencia = DateTime.Now;
                factura.MedioCodigo = Medio;
                factura.FileName = dr["facc_factura"].ToString().Trim().ToUpper() + "+" + dr["facv_tercero"].ToString().Trim();
                factura.Detalle = "Registro Oasis: " + dr["facc_factura"].ToString() + " Valor: " + dr["facv_total"].ToString() + " Nit del Prestador: " + dr["facv_tercero"].ToString() + " Responsable: " + dr["facv_responsable"].ToString() + " Unidad Almacenamiento: " + dr["facc_almacenamiento"].ToString() + " Modalidad de Contrato: " + dr["conc_nombre"].ToString();
                listDocuments.Add(factura);
            }
            return listDocuments;
        }
        catch (Exception ex)
        {
            log.Error($"Se ha presentado un error en CreateFacturaList2, {ex.Message} {ex.InnerException}");
            throw;
        }
    }
    private bool SaveTempFile(byte[] file, string fileName, out string name)
    {
        string path = string.Empty;
        try
        {
            name = "temp_" + DateTime.Now.Year + "_" + DateTime.Now.Month + "_" + DateTime.Now.Hour + "_" + DateTime.Now.Minute + "_" + DateTime.Now.Second + "_" + fileName;
            path = calculateTempPath();
            if (path == string.Empty)
            {
                name = string.Empty;
                return false;
            }
            File.WriteAllBytes(path + name, file);
            return true;
        }
        catch (Exception ex)
        {
            log.Error($"Se ha presentado un error en SaveTempFile {ex.Message} {ex.InnerException}");
            name = string.Empty;
            return false;
        }
    }
    private string calculateTempPath()
    {
        try
        {
            return AppDomain.CurrentDomain.BaseDirectory.ToString() + "temp/";
        }
        catch (Exception ex)
        {
            log.Error($"Se ha presentado un error en calculateTempPath {ex.Message} {ex.InnerException}");
            return string.Empty;
        }
    }
    private List<string> ValidateFacturaList(List<ObjFactura> objList)
    {
        List<ObjFactura> Nulos = new List<ObjFactura>();
        Nulos = objList.Where(x =>
        {
            int result;
            if (!string.IsNullOrWhiteSpace(x.Facn_empresa) && !string.IsNullOrWhiteSpace(x.Facc_documento) && !string.IsNullOrWhiteSpace(x.Facn_numero)
            && !string.IsNullOrWhiteSpace(x.Facn_ubicacion) && !string.IsNullOrWhiteSpace(x.Facv_tercero) && !double.IsNaN(x.Facv_total) && !string.IsNullOrWhiteSpace(x.Facc_estado)
            && !string.IsNullOrWhiteSpace(x.Facn_factura2) && !string.IsNullOrWhiteSpace(x.Facc_factura) && !string.IsNullOrWhiteSpace(x.Facc_alto_costo)
            && !string.IsNullOrWhiteSpace(x.Terc_nombre) && !string.IsNullOrWhiteSpace(x.Facn_recibo) && !string.IsNullOrWhiteSpace(x.Facv_copago)
            && !string.IsNullOrWhiteSpace(x.Facv_responsable) && !string.IsNullOrWhiteSpace(x.Facc_conciliado) && !string.IsNullOrWhiteSpace(x.Facv_imputable))
            {
                DateTime facf_confirmacion = x.Facf_confirmacion;
                if (!string.IsNullOrWhiteSpace(x.Facc_almacenamiento) && !string.IsNullOrWhiteSpace(x.Cntc_concepto))
                {
                    result = ((x.Conc_nombre == "") ? 1 : 0);
                    goto IL_017d;
                }
            }
            result = 1;
            goto IL_017d;
        IL_017d:
            return (byte)result != 0;
        }).ToList();
        List<string> Salida = new List<string>();
        if (Nulos.Count() > 0)
        {
            Salida.Add("Hay campos obligatorios sin diligenciar verifique su archivo. <br/> (Recuerde que los campos obligatorios son todos excepto: facc_prefijo, facf_radicado, y facf_final)");
        }
        return Salida;
    }
    private DataTable ReadDataFromFile(string fileName, out string result)
    {
        string path = string.Empty;
        try
        {
            path = calculateTempPath();
            if (path == string.Empty)
            {
                result = "Error en la ruta del archivo";
                return null;
            }
            string serverFileName = path + fileName;
            if (fileName.ToLower().EndsWith(".xls") || fileName.ToLower().EndsWith(".xlsx"))
            {
                string cadenaConexion = "Provider=Microsoft.ACE.OLEDB.12.0;Data Source=" + serverFileName + ";Persist Security Info=False;Extended Properties=Excel 8.0;";
                OleDbConnection oConn = new OleDbConnection(cadenaConexion);
                oConn.Open();
                OleDbCommand oCmd = new OleDbCommand("SELECT * FROM [ECFP$]", oConn);
                OleDbDataAdapter oDa = new OleDbDataAdapter();
                oDa.SelectCommand = oCmd;
                DataSet oDs = new DataSet();
                oDa.Fill(oDs, "Datos");
                oConn.Close();
                if (oDs.Tables[0].Rows.Count > 0)
                {
                    result = "Lectura exitosa";
                    return oDs.Tables[0];
                }
                result = "No hay datos para procesar";
                return null;
            }
            if (fileName.ToLower().EndsWith(".csv"))
            {
                DataTable oDs2 = new DataTable();
                oDs2 = ReadCSV(serverFileName);
                result = "Lectura exitosa";
                return oDs2;
            }
            result = "Formato no admitido";
            return null;
        }
        catch (Exception ex)
        {
            log.Error($"Se ha presentado un error en ReadDataFromFile {ex.Message} {ex.InnerException}");
            result = "Error desconocido";
            return null;
        }
    }
    private DataTable ReadCSV(string serverFileName)
    {
        FileHelperEngine engine = null;
        DataTable data = null;
        try
        {
            engine = new FileHelperEngine(typeof(ObjDocumentForCsv));
            ObjDocumentForCsv[] res = (ObjDocumentForCsv[])engine.ReadFile(serverFileName);
            data = new DataTable();
            data.Columns.Add(res[0].facn_empresa);
            data.Columns.Add(res[0].facc_documento);
            data.Columns.Add(res[0].facn_numero);
            data.Columns.Add(res[0].facn_ubicacion);
            data.Columns.Add(res[0].facv_tercero);
            data.Columns.Add(res[0].facv_total);
            data.Columns.Add(res[0].facc_estado);
            data.Columns.Add(res[0].facc_prefijo);
            data.Columns.Add(res[0].facn_factura2);
            data.Columns.Add(res[0].facc_factura);
            data.Columns.Add(res[0].facc_alto_costo);
            data.Columns.Add(res[0].terc_nombre);
            data.Columns.Add(res[0].facn_recibo);
            data.Columns.Add(res[0].facv_copago);
            data.Columns.Add(res[0].facv_responsable);
            data.Columns.Add(res[0].facc_conciliado);
            data.Columns.Add(res[0].facv_imputable);
            data.Columns.Add(res[0].facf_confirmacion);
            data.Columns.Add(res[0].facf_radicado);
            data.Columns.Add(res[0].facf_final);
            data.Columns.Add(res[0].facc_almacenamiento);
            data.Columns.Add(res[0].cntc_concepto);
            data.Columns.Add(res[0].conc_nombre);
            for (int i = 1; i < res.Length; i++)
            {
                DataRow row = data.NewRow();
                row[0] = res[i].facn_empresa;
                row[1] = res[i].facc_documento;
                row[2] = res[i].facn_numero;
                row[3] = res[i].facn_ubicacion;
                row[4] = res[i].facv_tercero;
                row[5] = res[i].facv_total;
                row[6] = res[i].facc_estado;
                row[7] = res[i].facc_prefijo;
                row[8] = res[i].facn_factura2;
                row[9] = res[i].facc_factura;
                row[10] = res[i].facc_alto_costo;
                row[11] = res[i].terc_nombre;
                row[12] = res[i].facn_recibo;
                row[13] = res[i].facv_copago;
                row[14] = res[i].facv_responsable;
                row[15] = res[i].facc_conciliado;
                row[16] = res[i].facv_imputable;
                row[17] = res[i].facf_confirmacion;
                row[18] = res[i].facf_radicado;
                row[19] = res[i].facf_final;
                row[20] = res[i].facc_almacenamiento;
                row[21] = res[i].cntc_concepto;
                row[22] = res[i].conc_nombre;
                data.Rows.Add(row);
            }
            return data;
        }
        catch (Exception ex)
        {
            log.Error($"Se ha presentado un error en CreateFacturaListFromCSV {ex.Message} {ex.InnerException}");
            throw;
        }
    }
}