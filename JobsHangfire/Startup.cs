using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Hangfire;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace JobsHangfire
{
    public class Startup
    {
        static HttpClient client = new HttpClient();

        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddHangfire(x => x.UseSqlServerStorage("Data Source=LAPTOP-70J3QRBR\\SQLEXPRESS;Initial Catalog=Hangfire;Trusted_Connection=True;"));
            services.AddMvc().SetCompatibilityVersion(CompatibilityVersion.Version_2_1);
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IBackgroundJobClient backgroundJobs, IHostingEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseHangfireServer();
            app.UseHangfireDashboard();

            backgroundJobs.Enqueue(() => Console.WriteLine("Hello world from Hangfire!"));

            RecurringJob.AddOrUpdate("AtualizarPedidos", () => RunAsync(), Cron.Minutely);

            app.UseMvc();
        }

        public class Pedido
        {
            public int IdPedido { get; set; }
            public int IdStatus { get; set; }
            public double ValorPedido { get; set; }
            public DateTime DataPrevisaoEntrega { get; set; }
        }

        static async Task<Pedido> GetPedido(string path)
        {
            Pedido pedido = null;
            HttpResponseMessage response = await client.GetAsync(path);
            if (response.IsSuccessStatusCode)
            {
                pedido = await response.Content.ReadAsAsync<Pedido>();
            }
            return pedido;
        }

        static async Task<Pedido> AtualizarPedido(string path, IEnumerable<Pedido> lista)
        {
            Pedido pedido = null;
            HttpResponseMessage response = await client.GetAsync(path); // Só um exemplo não está rodando
            if (response.IsSuccessStatusCode)
            {
                pedido = await response.Content.ReadAsAsync<Pedido>();
            }
            return pedido;
        }

       public static async Task RunAsync()
        {
            // Update port # in the following line.
            client.BaseAddress = new Uri("http://localhost:5000/pedido/");
            client.DefaultRequestHeaders.Accept.Clear();
            client.DefaultRequestHeaders.Accept.Add(
                new MediaTypeWithQualityHeaderValue("application/json"));

            try
            {
                // Get lista de pedidos
                var pedido = await GetPedido("http://localhost:5000/pedido/obter-todos");

                // Analisa os pedidos que ainda não foram processados
                // ...

                // Lista Atualizada
                IEnumerable<Pedido> lista = null;

                // Atualiza o histórico para que o pedido não fique "preso"
                var atualizar = await AtualizarPedido("http://localhost:5000/pedido/atualizar", lista);

            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }

            Console.ReadLine();
        }

    }
}
