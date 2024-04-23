from fastapi import FastAPI
from fastapi.middleware.cors import CORSMiddleware
import pyodbc
from fastapi.responses import JSONResponse

# instalacion
# python -m venv venv # esto solo se realiza una vez
# .\venv\Scripts\activate # este activa el entorno virtual del servidor
# pip install fastapi uvicorn pyodbc
# uvicorn main:app --reload # este inicia el servidor

# Crea una instancia de FastAPI
app = FastAPI()

# Configurar los origenes permitidos para todas las conexiones
origenes_permitidos = ["*"]

# Configurar la aplicación para permitir todas las conexiones
app.add_middleware(
    CORSMiddleware,
    allow_origins=origenes_permitidos,
    allow_credentials=True,
    allow_methods=["GET", "POST", "PUT", "DELETE"],
    allow_headers=["*"],
)
server = "GENESIS"
database = "RegistroCivil"
usuario = "Gene"
contraseña = "1234"


@app.get("/datos")
async def obtener_datos():

    try:
        conn = pyodbc.connect(
            "DRIVER={ODBC Driver 17 for SQL Server};SERVER="
            + server
            + ";DATABASE="
            + database
            + ";UID="
            + usuario
            + ";PWD="
            + contraseña
            + ";"
        )
        cursor = conn.cursor()
        cursor.execute("SELECT * FROM Registro")
        rows = cursor.fetchall()

        # Crear una lista de diccionarios con los datos
        data = []
        for row in rows:
            data.append({"Nombre": row[1], "Apellido": row[2], "Telefono": row[3], "Provincia": row[4], "Canton": row[5], "Distrito": row[6]})

        # Cerrar conexión a la base de datos
        conn.close()

        # Retornar los datos como una respuesta JSON
        return JSONResponse(content=data)

    except Exception as e:
        # Manejar cualquier error y retornar un mensaje de error
        return JSONResponse(content={"error": str(e)}, status_code=500)


@app.get("/datos/{identificacion}")
async def obtener_datos(identificacion: str):

    try:
        conn = pyodbc.connect(
            "DRIVER={ODBC Driver 17 for SQL Server};SERVER="
            + server
            + ";DATABASE="
            + database
            + ";UID="
            + usuario
            + ";PWD="
            + contraseña
            + ";"
        )
        cursor = conn.cursor()
        cursor.execute(
            "SELECT * FROM Registro WHERE Identificacion = ?", identificacion
        )
        rows = cursor.fetchall()

        # Crear una lista de diccionarios con los datos
        data = []
        for row in rows:
            data.append({"Nombre": row[1], "Apellido": row[2], "Telefono": row[3], "Provincia": row[4], "Canton": row[5], "Distrito": row[6]})

        # Cerrar conexión a la base de datos
        conn.close()

        # Retornar los datos como una respuesta JSON
        return data

    except Exception as e:
        # Manejar cualquier error y retornar un mensaje de error
        return JSONResponse(content={"error": str(e)}, status_code=500)