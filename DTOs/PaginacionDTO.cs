namespace PeliculasAPI.DTOs
{
    public class PaginacionDTO
    {
        public int Pagina { get; set; } = 1;

        private int recordsPorPagina = 10;
        private readonly int maxRecordsPorPagina = 50;

        public int RecordsPorPagina { 
            get { return recordsPorPagina; }

            set 
            {
                recordsPorPagina = (value > maxRecordsPorPagina) ? maxRecordsPorPagina : value;
            }
        
        }

    }
}
