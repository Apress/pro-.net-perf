// NativeTester.cpp : Defines the entry point for the console application.
//

#include "stdafx.h"

LARGE_INTEGER g_frequency;

template <typename Fn>
void time_it(Fn&& fn, const wchar_t* description) {
	LARGE_INTEGER begin, end;
	QueryPerformanceCounter(&begin);
	fn();
	QueryPerformanceCounter(&end);
	float elapsed = 1000.0f * (end.QuadPart - begin.QuadPart) / (float)g_frequency.QuadPart;
	wprintf(L"%s - %.2f ms\n", description, elapsed);
}

static volatile int a;

int wmain(int argc, wchar_t* argv[])
{
	QueryPerformanceFrequency(&g_frequency);
	for (int i = 0; i < 10; ++i) {
		time_it([] {
			int arr[100];
			for (int j = 0; j < 1000000; ++j) {
				int max0 = arr[0];
				for (int k = 1; k < 100; ++k)
				{
					max0 = max(max0, arr[k]);
				}
				a = max0;
			}
		}, L"Standard");
	}
	for (int i = 0; i < 10; ++i) {
		time_it([] {
			int arr[100];
			for (int j = 0; j < 1000000; ++j) {
				int max0 = arr[0];
				int max1 = arr[1];
				for (int k = 3; k < 100; k += 2)
				{
					max0 = max(max0, arr[k-1]);
					max1 = max(max1, arr[k]);
				}
				a = max(max0, max1);
			}
		}, L"Two local maxima");
	}
	for (int i = 0; i < 10; ++i) {
		time_it([] {
			int arr[100];
			for (int j = 0; j < 1000000; ++j) {
				int max0 = arr[0];
				int max1 = arr[1];
				int max2 = arr[2];
				int max3 = arr[3];
				for (int k = 5; k < 100; k += 4)
				{
					max0 = max(max0, arr[k-3]);
					max1 = max(max1, arr[k-2]);
					max2 = max(max2, arr[k-1]);
					max3 = max(max3, arr[k]);
				}
				a = max(max0, max(max1, max(max2, max3)));
			}
		}, L"Four local maxima");
	}
	for (int i = 0; i < 10; ++i) {
		time_it([] {
			int arr[100];
			for (int j = 0; j < 1000000; ++j) {
				__m128i max0 = *(__m128i*)arr;
				for (int k = 4; k < 100; k += 4)
				{
					max0 = _mm_max_epi32(max0, *(__m128i*)(arr+k));
				}
				int part0 = _mm_extract_epi32(max0, 0);
				int part1 = _mm_extract_epi32(max0, 1);
				int part2 = _mm_extract_epi32(max0, 2);
				int part3 = _mm_extract_epi32(max0, 3);
				a = max(part0, max(part1, max(part2, part3)));
			}
		}, L"With SSE4 (pmaxsd)");
	}
	

	return 0;
}

