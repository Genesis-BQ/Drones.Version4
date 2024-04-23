;
//asignar un nombre y versión al cache
const CACHE_NAME = 'v1_cache_drones',
    urlsToCache = [
        './',
        'https://fonts.googleapis.com/css2?family=Montserrat:wght@300&display=swap',
        './CSS/style.css',
        './CSS/Bloqueo.css',
        './CSS/boton.css',
        './CSS/Carritos.css',
        './CSS/Drones.css',
        './CSS/Error.css',
        './CSS/formulario.css',
        './CSS/Historial.css',
        './CSS/Homen.css',
        './CSS/Paypal.css',
        './CSS/Preguntas.css',
        './CSS/Registro.css',
        './CSS/RegistroCivil.css',
        './CSS/reset.css',
        './CSS/resultado.css',
        './CSS/Tarjeta.css',
        './CSS/TipoCambio.css',
        './CSS/Token.css',
        './CSS/Trackor.css',
        './CSS/Tranferencia.css',
        './Views/Home/Index.cshtml',
        './Views/Home/Home.cshtml',
        './Views/Home/ErrorRegistro.cshtml',
        './Views/Home/Drones.cshtml',
        './Views/Home/Traktor.cshtml',
        './Views/Home/Carrito.cshtml',
        './Views/Home/Paypal.cshtml',
        './Views/Home/Tajeta.cshtml',
        './Views/Home/Transferencias.cshtml',
        './Views/Home/RecuperarContraseña.cshtml',
        './Views/Home/Recuperar.cshtml',
        './Views/Home/Token.cshtml',
        './Views/Home/loginPaypal.cshtml',
        './Views/Home/RegistroCivil.cshtml',
        './Views/Home/UsuarioBloqueado.cshtml',
        './Views/Home/VerificalCodigo.cshtml',
        './Views/Home/TipoCambio.cshtml',
        './Views/Home/RestablecerContraseña.cshtml',
        './Views/Home/EnviarCorreo.cshtml',
        './Views/Home/ErrorinicioSesion.cshtml',
        './script.js',
        './Logo/logo-blanco.png',
        './Imagenes/Blanco.png',
        './Imagenes/black.jpg',
        './Imagenes/dron.jpg',
        './Imagenes/dron2.1.png',
        './Imagenes/drones.jpg',
        './Imagenes/drongif.gif',
        './Imagenes/incio.jpg',
        './Imagenes/logo-paypal.png',
        './Imagenes/personas.jpg',
        './Imagenes/tractor atardecer.jpg',
        './Imagenes/tractor.jpg'
    ]
//durante la fase de instalación, generalmente se almacena en caché los activos estáticos
self.addEventListener('install', e => {
    e.waitUntil(
        caches.open(CACHE_NAME)
            .then(cache => {
                return cache.addAll(urlsToCache)
                    .then(() => self.skipWaiting())
            })
            .catch(err => console.log('Falló registro de cache', err))
    )
})

//una vez que se instala el SW, se activa y busca los recursos para hacer que funcione sin conexión
self.addEventListener('activate', e => {
    const cacheWhitelist = [CACHE_NAME]

    e.waitUntil(
        caches.keys()
            .then(cacheNames => {
                return Promise.all(
                    cacheNames.map(cacheName => {
                        //Eliminamos lo que ya no se necesita en cache
                        if (cacheWhitelist.indexOf(cacheName) === -1) {
                            return caches.delete(cacheName)
                        }
                    })
                )
            })
            // Le indica al SW activar el cache actual
            .then(() => self.clients.claim())
    )
})

//cuando el navegador recupera una url
self.addEventListener('fetch', e => {
    //Responder ya sea con el objeto en caché o continuar y buscar la url real
    e.respondWith(
        caches.match(e.request)
            .then(res => {
                if (res) {
                    //recuperar del cache
                    return res
                }
                //recuperar de la petición a la url
                return fetch(e.request)
            })
    )
})