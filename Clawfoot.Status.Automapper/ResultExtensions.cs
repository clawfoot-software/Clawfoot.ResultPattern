using AutoMapper;
using Clawfoot.Result;
using System;
using System.Collections.Generic;
using System.Text;

namespace Clawfoot.Extensions.Automapper
{
    public static class ResultExtensions
    {
        public static Result<TToResult> MapResultTo<TFromResult, TToResult>(this Result<TFromResult> result, IMapper mapper)
        {
            TToResult resultValue = mapper.Map<TToResult>(result.Value);
            return result.To<TToResult>(resultValue);
        }
    }
}
