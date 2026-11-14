using System;
using System.Collections.Generic;
using System.Text;

using SQLite;
using SQLApp.Model;
using System.Collections.ObjectModel;

namespace SQLApp.ModelView
{
    public class CPessoaService
    {
        private SQLiteAsyncConnection _db;

        private IEnumerable<CPessoa> _pessoas;

        public ObservableCollection<CPessoa> PessoasFiltradas { get; private set; }

        public CPessoaService() 
        {
            Init();
        }

        public async Task Init()
        {
            if (_db != null)
                return;

            const string DatabaseFilename = "pessoas.db3";

            const SQLiteOpenFlags Flags =
                SQLite.SQLiteOpenFlags.ReadWrite |
                SQLite.SQLiteOpenFlags.Create |
                SQLite.SQLiteOpenFlags.SharedCache;

            string DatabasePath = Path.Combine(FileSystem.AppDataDirectory, 
                 DatabaseFilename);

            _db = new SQLiteAsyncConnection(DatabasePath, Flags);

            await _db.CreateTableAsync<CPessoa>();
        }

        public Task<int> InsertPessoa(CPessoa p) => _db.InsertAsync(p);

        public Task<int> UpdatePessoa(CPessoa p) => _db.UpdateAsync(p);

        public Task<int> DeletePessoa(CPessoa p) => _db.DeleteAsync(p);

        public Task<List<CPessoa>> GetPessoas() => _db.Table<CPessoa>().ToListAsync();

        public Task<List<CPessoa>> ConsultarID(string query)
        {
            string sql = $"SELECT * FROM CPessoa WHERE Id LIKE '%{query}%'";
            return _db.QueryAsync<CPessoa>(sql);
        }

        public Task<List<CPessoa>> ConsultarNome(string query)
        {
            string sql = $"SELECT * FROM CPessoa WHERE Nome LIKE '%{query}%'";
            return _db.QueryAsync<CPessoa>(sql);
        }

        public void OdernarPorNome(bool crescente = true)
        {
            if (_pessoas == null)
                _pessoas = _db.Table<CPessoa>().ToListAsync().Result;

            if (_pessoas.Count() == 0)
                return;

            var ordenadas = crescente
                ? _pessoas.OrderBy(p => p.Nome).ToList()
                : _pessoas.OrderByDescending(p => p.Nome).ToList();

            PessoasFiltradas = new ObservableCollection<CPessoa>(ordenadas);
        }

    }
}
